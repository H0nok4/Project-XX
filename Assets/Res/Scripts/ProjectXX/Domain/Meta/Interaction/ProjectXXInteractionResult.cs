namespace ProjectXX.Domain.Interaction
{
    public readonly struct ProjectXXInteractionResult
    {
        private ProjectXXInteractionResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static ProjectXXInteractionResult Success(string message)
        {
            return new ProjectXXInteractionResult(true, message);
        }

        public static ProjectXXInteractionResult Failed(string message)
        {
            return new ProjectXXInteractionResult(false, message);
        }
    }
}
