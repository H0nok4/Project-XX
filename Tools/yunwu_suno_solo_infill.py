#!/usr/bin/env python3
"""Submit uploaded-song remake jobs to the Yunwu Suno-compatible API.

Assumptions used here, based on the Apifox documentation:
- Local audio first goes through `/suno/uploads/audio` to obtain a `cover_clip_id`.
- Mid-song replacement is submitted through `/suno/submit/music` using:
  - `task="cover"`
  - `cover_clip_id=<uploaded clip id>`
  - `infill_start_s` / `infill_end_s`
  - `continued_aligned_prompt`
- Whole-song uploaded-audio remakes are submitted through the same endpoint using:
  - `task="cover"`
  - `cover_clip_id=<uploaded clip id>`
  - no infill fields
- The generated infill clip is merged back into the full song through
  `/suno/submit/concat` with `is_infill=true`.

The docs appear inconsistent in a few places and also mention `task=upload_extend`
and `mv=chirp-v4`. Because of that, this script keeps `--task` and `--mv`
overridable from the command line.
"""

from __future__ import annotations

import argparse
import json
import mimetypes
import os
import sys
import time
import uuid
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any
from urllib import error, request


BASE_URL = "https://yunwu.ai"
DEFAULT_AUDIO_PATH = Path(
    r"C:\Users\Administrator\Downloads\トゲナシトゲアリ - 空の箱 (井芹仁菜、河原木桃香) (2).mp3"
)
DEFAULT_OUTPUT_DIR = Path("Temp/yunwu_suno_solo_runs")
DEFAULT_TIMEOUT_SECONDS = 60 * 12
DEFAULT_POLL_SECONDS = 8
DEFAULT_MV = "chirp-v3-5-tau"
DEFAULT_TASK = "cover"
DEFAULT_TITLE_PREFIX = "Sora no Hako - New Guitar Solo"
DEFAULT_MODE = "full-cover"
DEFAULT_FULL_COVER_TITLE = "Sora no Hako - Metal Instrumental Remake"
DEFAULT_FULL_COVER_PROMPT = (
    "Remake the entire song into a tighter, heavier instrumental metal arrangement. "
    "Target roughly 160 BPM while preserving the emotional contour and melodic identity "
    "of the source. Use only lead electric guitar, rhythm electric guitars, bass, and "
    "drums. Push the drums with strong kick presence and crisp snare, make the rhythm "
    "guitars palm-muted and aggressive, let the lead guitar carry the main hooks, and "
    "keep the bass locked with the riffs. No vocals, no choirs, no spoken word, no synths."
)
DEFAULT_FULL_COVER_TAGS = (
    "metal, heavy metal, melodic metal, instrumental, around 160 bpm, lead electric guitar, "
    "rhythm electric guitars, bass guitar, acoustic drums, tight mix"
)
DEFAULT_FULL_COVER_NEGATIVE_TAGS = (
    "vocals, singing, rap, spoken word, choir, strings, piano, synth, EDM, ambient intro"
)


@dataclass(frozen=True)
class SoloSegment:
    name: str
    start_s: float
    end_s: float
    title: str
    prompt: str
    continued_aligned_prompt: str
    tags: str
    negative_tags: str


@dataclass(frozen=True)
class FullCoverRequest:
    title: str
    prompt: str
    tags: str
    negative_tags: str


DEFAULT_SEGMENTS = [
    SoloSegment(
        name="solo_a",
        start_s=79.0,
        end_s=86.0,
        title=f"{DEFAULT_TITLE_PREFIX} A",
        prompt=(
            "Replace this section with a fresh electric guitar solo that matches the "
            "original song's key, tempo, drum groove, bass movement, and emotional "
            "anime rock feel. Keep it instrumental, expressive, and melodic."
        ),
        continued_aligned_prompt=(
            "At 01:19-01:26, insert a new electric guitar solo. Begin with a lyrical "
            "lead phrase, add tasteful bends and vibrato, then finish with a tight "
            "resolve that hands naturally back to the next section. No vocals inside "
            "the solo window."
        ),
        tags=(
            "anime rock, j-rock, female vocal rock band, electric guitar solo, "
            "emotional lead guitar, live drums, energetic bass"
        ),
        negative_tags=(
            "vocals, singing, rap, spoken word, saxophone, synth lead, EDM drop"
        ),
    ),
    SoloSegment(
        name="solo_b",
        start_s=123.0,
        end_s=139.0,
        title=f"{DEFAULT_TITLE_PREFIX} B",
        prompt=(
            "Replace this section with a larger and more climactic electric guitar "
            "solo while preserving the original arrangement, harmonic center, and "
            "tempo. Keep the tone emotional, band-driven, and coherent with the song."
        ),
        continued_aligned_prompt=(
            "At 02:03-02:19, generate a longer electric guitar solo with rising "
            "intensity. Start melodic, then build into faster phrasing, octave runs, "
            "and a strong sustained note before resolving cleanly back into the song. "
            "No vocals inside the solo window."
        ),
        tags=(
            "anime rock, j-rock, emotional band sound, climactic electric guitar solo, "
            "melodic shred, live drums, dynamic arrangement"
        ),
        negative_tags=(
            "vocals, singing, rap, spoken word, brass, synth arpeggio, EDM drop"
        ),
    ),
]


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description=(
            "Upload a local song to Yunwu, initialize its clip, and submit either "
            "two solo infill jobs or a whole-song remake."
        )
    )
    parser.add_argument(
        "--mode",
        choices=("full-cover", "solo-infill"),
        default=DEFAULT_MODE,
        help=f"Generation mode. Default: {DEFAULT_MODE}",
    )
    parser.add_argument(
        "--token",
        default=os.environ.get("YUNWU_API_TOKEN", ""),
        help="Yunwu bearer token. Defaults to the YUNWU_API_TOKEN environment variable.",
    )
    parser.add_argument(
        "--audio",
        type=Path,
        default=DEFAULT_AUDIO_PATH,
        help=f"Local source audio path. Defaults to: {DEFAULT_AUDIO_PATH}",
    )
    parser.add_argument(
        "--cover-clip-id",
        default="",
        help="Existing cover clip id. If provided, upload and initialize are skipped.",
    )
    parser.add_argument(
        "--task",
        default=DEFAULT_TASK,
        help=(
            f"Generation task for uploaded-song editing. Defaults to {DEFAULT_TASK!r}. "
            "Change to upload_extend only if your account/doc variant requires it."
        ),
    )
    parser.add_argument(
        "--mv",
        default=DEFAULT_MV,
        help=(
            f"Model version. Defaults to {DEFAULT_MV!r}. Change to chirp-v4 if your "
            "account/doc variant requires it."
        ),
    )
    parser.add_argument(
        "--poll-seconds",
        type=float,
        default=DEFAULT_POLL_SECONDS,
        help=f"Polling interval in seconds. Default: {DEFAULT_POLL_SECONDS}",
    )
    parser.add_argument(
        "--timeout-seconds",
        type=float,
        default=DEFAULT_TIMEOUT_SECONDS,
        help=f"Polling timeout in seconds. Default: {DEFAULT_TIMEOUT_SECONDS}",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help=f"Run output directory. Default: {DEFAULT_OUTPUT_DIR}",
    )
    parser.add_argument(
        "--skip-concat",
        action="store_true",
        help="Submit infill jobs only, without calling /suno/submit/concat.",
    )
    parser.add_argument(
        "--title",
        default=DEFAULT_FULL_COVER_TITLE,
        help=f"Whole-song remake title. Default: {DEFAULT_FULL_COVER_TITLE!r}",
    )
    parser.add_argument(
        "--prompt",
        default=DEFAULT_FULL_COVER_PROMPT,
        help="Whole-song remake prompt.",
    )
    parser.add_argument(
        "--tags",
        default=DEFAULT_FULL_COVER_TAGS,
        help="Whole-song remake style tags.",
    )
    parser.add_argument(
        "--negative-tags",
        default=DEFAULT_FULL_COVER_NEGATIVE_TAGS,
        help="Whole-song remake negative tags.",
    )
    parser.add_argument(
        "--download-results",
        action="store_true",
        help="Download generated MP3 files into the run directory.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the payloads that would be sent, without making network requests.",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()

    if not args.dry_run and not args.token:
        print("Missing token. Use --token or set YUNWU_API_TOKEN.", file=sys.stderr)
        return 2

    run_dir = args.output_dir / time.strftime("%Y%m%d-%H%M%S")
    run_dir.mkdir(parents=True, exist_ok=True)

    if args.mode == "solo-infill":
        write_json(run_dir / "segments.json", [asdict(segment) for segment in DEFAULT_SEGMENTS])
    else:
        write_json(
            run_dir / "full_cover_request.json",
            asdict(
                FullCoverRequest(
                    title=args.title,
                    prompt=args.prompt,
                    tags=args.tags,
                    negative_tags=args.negative_tags,
                )
            ),
        )

    if args.dry_run:
        payloads: Any
        if args.mode == "solo-infill":
            payloads = [
                build_music_payload(
                    cover_clip_id=args.cover_clip_id or "<cover_clip_id>",
                    task=args.task,
                    mv=args.mv,
                    segment=segment,
                )
                for segment in DEFAULT_SEGMENTS
            ]
        else:
            payloads = build_full_cover_payload(
                cover_clip_id=args.cover_clip_id or "<cover_clip_id>",
                task=args.task,
                mv=args.mv,
                request_spec=FullCoverRequest(
                    title=args.title,
                    prompt=args.prompt,
                    tags=args.tags,
                    negative_tags=args.negative_tags,
                ),
            )
        write_json(run_dir / "dry_run_payloads.json", payloads)
        print(f"Dry run only. Payloads written to: {run_dir}")
        print(json.dumps(payloads, ensure_ascii=False, indent=2))
        return 0

    audio_path = args.audio.expanduser().resolve()
    if not audio_path.is_file() and not args.cover_clip_id:
        print(f"Audio file not found: {audio_path}", file=sys.stderr)
        return 2

    try:
        cover_clip_id = args.cover_clip_id or upload_and_initialize_clip(
            token=args.token,
            audio_path=audio_path,
            run_dir=run_dir,
            poll_seconds=args.poll_seconds,
            timeout_seconds=args.timeout_seconds,
        )
        summary: dict[str, Any] = {
            "audio_path": str(audio_path),
            "cover_clip_id": cover_clip_id,
            "mode": args.mode,
            "task": args.task,
            "mv": args.mv,
        }

        if args.mode == "solo-infill":
            summary["segments"] = []
            for segment in DEFAULT_SEGMENTS:
                print(
                    f"Submitting {segment.name} ({format_time(segment.start_s)}-"
                    f"{format_time(segment.end_s)})..."
                )
                segment_summary = submit_segment_flow(
                    token=args.token,
                    cover_clip_id=cover_clip_id,
                    task=args.task,
                    mv=args.mv,
                    segment=segment,
                    run_dir=run_dir,
                    poll_seconds=args.poll_seconds,
                    timeout_seconds=args.timeout_seconds,
                    skip_concat=args.skip_concat,
                    download_results=args.download_results,
                )
                summary["segments"].append(segment_summary)
        else:
            request_spec = FullCoverRequest(
                title=args.title,
                prompt=args.prompt,
                tags=args.tags,
                negative_tags=args.negative_tags,
            )
            summary["full_cover"] = submit_full_cover_flow(
                token=args.token,
                cover_clip_id=cover_clip_id,
                task=args.task,
                mv=args.mv,
                request_spec=request_spec,
                run_dir=run_dir,
                poll_seconds=args.poll_seconds,
                timeout_seconds=args.timeout_seconds,
                download_results=args.download_results,
            )

        write_json(run_dir / "summary.json", summary)
        print(f"Done. Summary written to: {run_dir / 'summary.json'}")
        return 0
    except Exception as exc:  # pragma: no cover - surfaced to the user directly
        print(f"Failed: {exc}", file=sys.stderr)
        return 1


def upload_and_initialize_clip(
    token: str,
    audio_path: Path,
    run_dir: Path,
    poll_seconds: float,
    timeout_seconds: float,
) -> str:
    print(f"Requesting upload credentials for: {audio_path.name}")
    upload_auth_response = api_json(
        method="POST",
        url=f"{BASE_URL}/suno/uploads/audio",
        token=token,
        payload={"extension": audio_path.suffix.lstrip(".").lower()},
    )
    write_json(run_dir / "01_upload_auth.json", upload_auth_response)

    upload_spec = extract_upload_spec(upload_auth_response)
    upload_file(upload_spec, audio_path)

    print(f"Marking upload as finished: {upload_spec['id']}")
    finish_response = api_json(
        method="POST",
        url=f"{BASE_URL}/suno/uploads/audio/{upload_spec['id']}/upload-finish",
        token=token,
        payload={
            "upload_type": "file_upload",
            "upload_filename": audio_path.name,
        },
    )
    write_json(run_dir / "02_upload_finish.json", finish_response)

    print("Waiting for uploaded audio to finish processing...")
    upload_status = poll_until_terminal(
        label="upload",
        fetcher=lambda: api_json(
            method="GET",
            url=f"{BASE_URL}/suno/uploads/audio/{upload_spec['id']}",
            token=token,
        ),
        poll_seconds=poll_seconds,
        timeout_seconds=timeout_seconds,
    )
    write_json(run_dir / "03_upload_status.json", upload_status)

    print("Initializing clip from uploaded audio...")
    initialize_response = api_json(
        method="POST",
        url=f"{BASE_URL}/suno/uploads/audio/{upload_spec['id']}/initialize-clip",
        token=token,
        payload={},
    )
    write_json(run_dir / "04_initialize_clip.json", initialize_response)

    cover_clip_id = extract_primary_clip_id(initialize_response)
    if not cover_clip_id:
        raise RuntimeError(
            "Could not extract a clip id from initialize-clip response. "
            "Inspect the saved JSON files and update the parser if needed."
        )

    print(f"Using cover clip id: {cover_clip_id}")
    return cover_clip_id


def submit_segment_flow(
    token: str,
    cover_clip_id: str,
    task: str,
    mv: str,
    segment: SoloSegment,
    run_dir: Path,
    poll_seconds: float,
    timeout_seconds: float,
    skip_concat: bool,
    download_results: bool,
) -> dict[str, Any]:
    segment_dir = run_dir / segment.name
    segment_dir.mkdir(parents=True, exist_ok=True)

    payload = build_music_payload(
        cover_clip_id=cover_clip_id,
        task=task,
        mv=mv,
        segment=segment,
    )
    write_json(segment_dir / "01_submit_payload.json", payload)

    submit_response = api_json(
        method="POST",
        url=f"{BASE_URL}/suno/submit/music",
        token=token,
        payload=payload,
    )
    write_json(segment_dir / "02_submit_response.json", submit_response)

    task_id = extract_task_id(submit_response)
    if not task_id:
        raise RuntimeError(
            f"Could not extract a task id for {segment.name}. Inspect "
            f"{segment_dir / '02_submit_response.json'}."
        )

    task_result = poll_until_terminal(
        label=f"music:{segment.name}",
        fetcher=lambda: api_json(
            method="GET",
            url=f"{BASE_URL}/suno/fetch/{task_id}",
            token=token,
        ),
        poll_seconds=poll_seconds,
        timeout_seconds=timeout_seconds,
    )
    write_json(segment_dir / "03_music_result.json", task_result)

    generated_variants = extract_song_variants(task_result, task_id=task_id)
    generated_clip_id = generated_variants[0]["clip_id"] if generated_variants else ""
    segment_summary: dict[str, Any] = {
        "name": segment.name,
        "start_s": segment.start_s,
        "end_s": segment.end_s,
        "submit_task_id": task_id,
        "generated_clip_id": generated_clip_id,
        "generated_variants": generated_variants,
    }

    if download_results and generated_variants:
        download_dir = segment_dir / "downloads"
        downloads = download_song_variants(generated_variants, download_dir, prefix=segment.name)
        segment_summary["downloaded_files"] = downloads

    if skip_concat:
        return segment_summary

    if not generated_clip_id:
        raise RuntimeError(
            f"Could not extract a generated clip id for {segment.name}. Inspect "
            f"{segment_dir / '03_music_result.json'}."
        )

    concat_payload = {"clip_id": generated_clip_id, "is_infill": True}
    write_json(segment_dir / "04_concat_payload.json", concat_payload)

    concat_submit = api_json(
        method="POST",
        url=f"{BASE_URL}/suno/submit/concat",
        token=token,
        payload=concat_payload,
    )
    write_json(segment_dir / "05_concat_submit.json", concat_submit)

    concat_task_id = extract_task_id(concat_submit)
    segment_summary["concat_task_id"] = concat_task_id

    if not concat_task_id:
        return segment_summary

    concat_result = poll_until_terminal(
        label=f"concat:{segment.name}",
        fetcher=lambda: api_json(
            method="GET",
            url=f"{BASE_URL}/suno/fetch/{concat_task_id}",
            token=token,
        ),
        poll_seconds=poll_seconds,
        timeout_seconds=timeout_seconds,
    )
    write_json(segment_dir / "06_concat_result.json", concat_result)

    segment_summary["final_clip_id"] = extract_primary_clip_id(
        concat_result, task_id=concat_task_id
    )
    return segment_summary


def submit_full_cover_flow(
    token: str,
    cover_clip_id: str,
    task: str,
    mv: str,
    request_spec: FullCoverRequest,
    run_dir: Path,
    poll_seconds: float,
    timeout_seconds: float,
    download_results: bool,
) -> dict[str, Any]:
    payload = build_full_cover_payload(
        cover_clip_id=cover_clip_id,
        task=task,
        mv=mv,
        request_spec=request_spec,
    )
    write_json(run_dir / "01_submit_payload.json", payload)

    submit_response = api_json(
        method="POST",
        url=f"{BASE_URL}/suno/submit/music",
        token=token,
        payload=payload,
    )
    write_json(run_dir / "02_submit_response.json", submit_response)

    task_id = extract_task_id(submit_response)
    if not task_id:
        raise RuntimeError(
            f"Could not extract a task id for the whole-song remake. Inspect "
            f"{run_dir / '02_submit_response.json'}."
        )

    task_result = poll_until_terminal(
        label="music:full-cover",
        fetcher=lambda: api_json(
            method="GET",
            url=f"{BASE_URL}/suno/fetch/{task_id}",
            token=token,
        ),
        poll_seconds=poll_seconds,
        timeout_seconds=timeout_seconds,
    )
    write_json(run_dir / "03_music_result.json", task_result)

    generated_variants = extract_song_variants(task_result, task_id=task_id)
    summary: dict[str, Any] = {
        "title": request_spec.title,
        "submit_task_id": task_id,
        "generated_clip_id": generated_variants[0]["clip_id"] if generated_variants else "",
        "generated_variants": generated_variants,
    }

    if download_results and generated_variants:
        download_dir = run_dir / "downloads"
        downloads = download_song_variants(generated_variants, download_dir, prefix="full_cover")
        summary["downloaded_files"] = downloads

    return summary


def build_music_payload(
    cover_clip_id: str,
    task: str,
    mv: str,
    segment: SoloSegment,
) -> dict[str, Any]:
    return {
        "title": segment.title,
        "prompt": segment.prompt,
        "generation_type": "TEXT",
        "tags": segment.tags,
        "negative_tags": segment.negative_tags,
        "mv": mv,
        "continue_clip_id": None,
        "continue_at": None,
        "continued_aligned_prompt": segment.continued_aligned_prompt,
        "infill_start_s": segment.start_s,
        "infill_end_s": segment.end_s,
        "task": task,
        "cover_clip_id": cover_clip_id,
    }


def build_full_cover_payload(
    cover_clip_id: str,
    task: str,
    mv: str,
    request_spec: FullCoverRequest,
) -> dict[str, Any]:
    return {
        "title": request_spec.title,
        "prompt": request_spec.prompt,
        "generation_type": "TEXT",
        "tags": request_spec.tags,
        "negative_tags": request_spec.negative_tags,
        "mv": mv,
        "continue_clip_id": None,
        "continue_at": None,
        "continued_aligned_prompt": None,
        "infill_start_s": None,
        "infill_end_s": None,
        "task": task,
        "cover_clip_id": cover_clip_id,
    }


def upload_file(upload_spec: dict[str, Any], audio_path: Path) -> None:
    file_bytes = audio_path.read_bytes()
    fields = upload_spec["fields"]
    file_content_type = (
        fields.get("Content-Type")
        or mimetypes.guess_type(audio_path.name)[0]
        or "application/octet-stream"
    )
    boundary = f"----CodexBoundary{uuid.uuid4().hex}"
    body = build_multipart_body(
        boundary=boundary,
        fields=fields,
        file_name=audio_path.name,
        file_bytes=file_bytes,
        file_content_type=file_content_type,
    )
    req = request.Request(
        upload_spec["url"],
        data=body,
        method="POST",
        headers={"Content-Type": f"multipart/form-data; boundary={boundary}"},
    )
    try:
        with request.urlopen(req, timeout=120) as response:
            if response.status not in {200, 201, 204}:
                raise RuntimeError(f"Unexpected upload status: {response.status}")
    except error.HTTPError as exc:
        details = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"S3 upload failed: {exc.code} {details}") from exc


def build_multipart_body(
    boundary: str,
    fields: dict[str, Any],
    file_name: str,
    file_bytes: bytes,
    file_content_type: str,
) -> bytes:
    boundary_bytes = boundary.encode("utf-8")
    body = bytearray()

    for name, value in fields.items():
        body.extend(b"--" + boundary_bytes + b"\r\n")
        body.extend(
            f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode("utf-8")
        )
        body.extend(str(value).encode("utf-8"))
        body.extend(b"\r\n")

    body.extend(b"--" + boundary_bytes + b"\r\n")
    body.extend(
        (
            f'Content-Disposition: form-data; name="file"; filename="{file_name}"\r\n'
            f"Content-Type: {file_content_type}\r\n\r\n"
        ).encode("utf-8")
    )
    body.extend(file_bytes)
    body.extend(b"\r\n")
    body.extend(b"--" + boundary_bytes + b"--\r\n")
    return bytes(body)


def api_json(
    method: str,
    url: str,
    token: str,
    payload: dict[str, Any] | None = None,
) -> dict[str, Any]:
    data = None
    headers = {"Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    if payload is not None:
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        headers["Content-Type"] = "application/json"
    req = request.Request(url, data=data, method=method, headers=headers)

    try:
        with request.urlopen(req, timeout=60) as response:
            raw = response.read().decode("utf-8", errors="replace")
    except error.HTTPError as exc:
        details = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"{method} {url} failed: {exc.code} {details}") from exc

    if not raw.strip():
        return {}

    try:
        return json.loads(raw)
    except json.JSONDecodeError as exc:
        raise RuntimeError(f"Expected JSON from {url}, got: {raw[:500]}") from exc


def poll_until_terminal(
    label: str,
    fetcher: callable,
    poll_seconds: float,
    timeout_seconds: float,
) -> dict[str, Any]:
    started_at = time.time()
    last_response: dict[str, Any] | None = None

    while True:
        response = fetcher()
        last_response = response
        state = normalize_state(response)
        print(f"[{label}] state={state}")

        if is_success_state(state):
            return response
        if is_failure_state(state):
            raise RuntimeError(f"{label} failed with state={state}: {response}")

        if time.time() - started_at > timeout_seconds:
            raise TimeoutError(
                f"Timed out while waiting for {label}. Last response: {last_response}"
            )

        time.sleep(poll_seconds)


def normalize_state(response: dict[str, Any]) -> str:
    candidates = list(iter_dicts(response))
    data = response.get("data")
    if isinstance(data, list):
        candidates.extend(item for item in data if isinstance(item, dict))

    for candidate in candidates:
        for key in ("status", "state", "task_status"):
            value = candidate.get(key)
            if isinstance(value, str) and value.strip():
                return value.strip().upper()

    code = response.get("code")
    if isinstance(code, str) and code.strip():
        return code.strip().upper()

    return "UNKNOWN"


def is_success_state(state: str) -> bool:
    return state in {"SUCCESS", "COMPLETED", "COMPLETE"}


def is_failure_state(state: str) -> bool:
    return state in {"FAILURE", "FAILED", "ERROR", "CANCELED", "CANCELLED"}


def extract_upload_spec(response: dict[str, Any]) -> dict[str, Any]:
    data = response.get("data") if isinstance(response.get("data"), dict) else response
    if not isinstance(data, dict):
        raise RuntimeError(f"Unexpected upload auth response: {response}")

    upload_id = data.get("id") or data.get("upload_id")
    upload_url = data.get("url") or data.get("upload_url")
    fields = data.get("fields") or data.get("formData")
    if not upload_id or not upload_url or not isinstance(fields, dict):
        raise RuntimeError(f"Incomplete upload auth response: {response}")

    return {"id": upload_id, "url": upload_url, "fields": fields}


def extract_task_id(response: dict[str, Any]) -> str:
    value = response.get("data")
    if isinstance(value, str):
        return value
    if isinstance(value, dict):
        for key in ("task_id", "id"):
            inner = value.get(key)
            if isinstance(inner, str):
                return inner
    return ""


def extract_primary_clip_id(response: Any, task_id: str | None = None) -> str:
    if isinstance(response, dict):
        data = response.get("data")
        if isinstance(data, str) and data and data != task_id:
            return data

        for candidate in iter_dicts(response):
            clip_id = candidate.get("clip_id")
            if isinstance(clip_id, str) and clip_id:
                return clip_id

            if isinstance(candidate.get("audio_url"), str):
                object_id = candidate.get("id")
                if isinstance(object_id, str) and object_id != task_id:
                    return object_id

        if isinstance(data, list):
            for item in data:
                if isinstance(item, dict):
                    clip_id = extract_primary_clip_id(item, task_id=task_id)
                    if clip_id:
                        return clip_id

    return ""


def extract_song_variants(response: Any, task_id: str | None = None) -> list[dict[str, Any]]:
    variants: list[dict[str, Any]] = []
    seen_clip_ids: set[str] = set()

    for candidate in iter_dicts(response):
        clip_id = candidate.get("clip_id")
        audio_url = candidate.get("audio_url")
        if not isinstance(clip_id, str) or not clip_id or clip_id == task_id:
            continue
        if not isinstance(audio_url, str) or not audio_url:
            continue
        if clip_id in seen_clip_ids:
            continue

        variants.append(
            {
                "clip_id": clip_id,
                "audio_url": audio_url,
                "title": candidate.get("title", ""),
                "duration": candidate.get("duration"),
                "batch_index": candidate.get("batch_index"),
                "video_url": candidate.get("video_url", ""),
            }
        )
        seen_clip_ids.add(clip_id)

    variants.sort(key=lambda item: sort_key_nullable_int(item.get("batch_index")))
    return variants


def sort_key_nullable_int(value: Any) -> tuple[int, int]:
    if isinstance(value, int):
        return (0, value)
    return (1, 0)


def iter_dicts(node: Any):
    if isinstance(node, dict):
        yield node
        for value in node.values():
            yield from iter_dicts(value)
    elif isinstance(node, list):
        for item in node:
            yield from iter_dicts(item)


def format_time(seconds: float) -> str:
    total_seconds = int(seconds)
    minutes = total_seconds // 60
    remaining = total_seconds % 60
    return f"{minutes:02d}:{remaining:02d}"


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def download_song_variants(
    variants: list[dict[str, Any]],
    download_dir: Path,
    prefix: str,
) -> list[dict[str, Any]]:
    downloads: list[dict[str, Any]] = []
    download_dir.mkdir(parents=True, exist_ok=True)

    for index, variant in enumerate(variants, start=1):
        clip_id = variant["clip_id"]
        file_name = f"{prefix}_{index:02d}_{clip_id[:8]}.mp3"
        output_path = download_dir / file_name
        download_binary(variant["audio_url"], output_path)
        downloads.append(
            {
                "clip_id": clip_id,
                "audio_url": variant["audio_url"],
                "path": str(output_path.resolve()),
            }
        )

    return downloads


def download_binary(url: str, output_path: Path) -> None:
    req = request.Request(url, method="GET")
    try:
        with request.urlopen(req, timeout=120) as response:
            data = response.read()
    except error.HTTPError as exc:
        details = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"GET {url} failed: {exc.code} {details}") from exc

    output_path.write_bytes(data)


if __name__ == "__main__":
    raise SystemExit(main())
