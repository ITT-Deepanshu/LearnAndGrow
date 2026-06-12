using FluentAssertions;
using PRM.Infrastructure.Ai;

namespace PRM.UnitTests.Infrastructure;

public class AiResponseParserTeamSkillMatchTests
{
    [Fact]
    public void ParseTeamSkillMatch_RejectsDuplicateAssignmentsAndInvalidIds()
    {
        const string json = """
            {
              "teamDefined": [{"roleTitle":"DevOps Engineer","count":1,"requiredSkills":[{"name":"Docker","minProficiency":"Beginner"}]}],
              "assignments": [
                {"roleTitle":"DevOps Engineer","slotNumber":1,"employeeId":10,"why":"Good fit"},
                {"roleTitle":"Frontend Engineer","slotNumber":1,"employeeId":10,"why":"Duplicate"},
                {"roleTitle":"Frontend Engineer","slotNumber":1,"employeeId":99,"why":"Invalid id"}
              ],
              "unfilled": [{"roleTitle":"Backend Developer","unfilledCount":2,"reason":"Not enough","detail":"Gap detail"}]
            }
            """;

        var result = AiResponseParser.ParseTeamSkillMatch(json, new HashSet<long> { 10, 11 });

        result.Assignments.Should().ContainSingle(a => a.ResourceProfileId == 10);
        result.Unfilled.Should().ContainSingle(u => u.UnfilledCount == 2);
        result.TeamDefined.Should().ContainSingle(r => r.RoleTitle == "DevOps Engineer");
    }
}
