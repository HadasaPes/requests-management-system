using RequestsBL.Rules;
using RequestsDAL.Entities;

namespace RequestsApi.Tests.Unit;

public class StatusTransitionsTests
{
    [Theory]
    // Allowed
    [InlineData(RequestStatus.New, RequestStatus.InProgress, true)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Waiting, true)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed, true)]
    [InlineData(RequestStatus.Waiting, RequestStatus.InProgress, true)]
    // Forbidden: skipping steps, going back, leaving Completed, staying the same
    [InlineData(RequestStatus.New, RequestStatus.Waiting, false)]
    [InlineData(RequestStatus.New, RequestStatus.Completed, false)]
    [InlineData(RequestStatus.InProgress, RequestStatus.New, false)]
    [InlineData(RequestStatus.Waiting, RequestStatus.Completed, false)]
    [InlineData(RequestStatus.Waiting, RequestStatus.New, false)]
    [InlineData(RequestStatus.Completed, RequestStatus.New, false)]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress, false)]
    [InlineData(RequestStatus.Completed, RequestStatus.Waiting, false)]
    [InlineData(RequestStatus.New, RequestStatus.New, false)]
    [InlineData(RequestStatus.Completed, RequestStatus.Completed, false)]
    public void GetError_Transition_ReturnsNullOnlyWhenAllowed(RequestStatus from, RequestStatus to, bool expectedAllowed)
    {
        // Act
        var error = StatusTransitions.GetError(from, to);

        // Assert
        Assert.Equal(expectedAllowed, error is null);
        Assert.Equal(expectedAllowed, StatusTransitions.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(RequestStatus.New, new[] { RequestStatus.InProgress })]
    [InlineData(RequestStatus.InProgress, new[] { RequestStatus.Waiting, RequestStatus.Completed })]
    [InlineData(RequestStatus.Waiting, new[] { RequestStatus.InProgress })]
    [InlineData(RequestStatus.Completed, new RequestStatus[0])]
    public void GetAllowedNext_Status_ReturnsExactlyTheAllowedTransitions(RequestStatus from, RequestStatus[] expected)
    {
        // Act
        var allowedNext = StatusTransitions.GetAllowedNext(from);

        // Assert
        Assert.Equal(expected, allowedNext);
    }
}
