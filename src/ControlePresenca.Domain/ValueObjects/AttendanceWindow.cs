namespace ControlePresenca.Domain.ValueObjects;

public sealed class AttendanceWindow
{
    public TimeOnly StartAt { get; private set; }
    public TimeOnly EndAt { get; private set; }

    public AttendanceWindow(TimeOnly startAt, TimeOnly endAt)
    {
        if (endAt <= startAt)
            throw new ArgumentException("O horário final deve ser posterior ao horário inicial.");

        StartAt = startAt;
        EndAt = endAt;
    }

    public bool Contains(TimeOnly time) => time >= StartAt && time <= EndAt;
}