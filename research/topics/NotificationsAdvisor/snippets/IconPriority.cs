namespace Game.Notifications;

public enum IconPriority : byte
{
	Min = 0,
	Info = 10,
	Problem = 50,
	Warning = 100,
	MajorProblem = 150,
	Error = 200,
	FatalProblem = 250,
	Max = byte.MaxValue
}
