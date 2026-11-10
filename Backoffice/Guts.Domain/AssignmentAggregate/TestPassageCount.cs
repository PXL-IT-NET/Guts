namespace Guts.Domain.AssignmentAggregate;

public class TestPassageCount
{
    public int AmountOfUsers { get; }
    public int AmountOfPassedTests { get; }

    public TestPassageCount(int amountOfUsers, int amountOfPassedTests)
    {
        AmountOfUsers = amountOfUsers;
        AmountOfPassedTests = amountOfPassedTests;
    }
}

