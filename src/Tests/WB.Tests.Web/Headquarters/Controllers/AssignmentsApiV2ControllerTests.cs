using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Users;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Tests.Abc;
using WB.UI.Headquarters.Controllers.Api.DataCollection.Interviewer.v2;

namespace WB.Tests.Unit.BoundedContexts.Headquarters.Assignments
{
    public class AssignmentsApiV2ControllerTests
    {
        [Test]
        public async Task should_return_assignmentsList_accounting_created_interviews()
        {
            var assignmentsList = new List<Assignment>
            {
                Create.Entity.Assignment(quantity: 10, interviewSummary: new HashSet<InterviewSummary>
                {
                    Create.Entity.InterviewSummary(status: InterviewStatus.Completed),
                    Create.Entity.InterviewSummary(status: InterviewStatus.Completed),

                    Create.Entity.InterviewSummary(status: InterviewStatus.InterviewerAssigned),

                    Create.Entity.InterviewSummary(status: InterviewStatus.RejectedByHeadquarters),
                    Create.Entity.InterviewSummary(status: InterviewStatus.RejectedBySupervisor)
                })
            };

            var assignmentService = Mock.Of<IAssignmentsService>(s => s.GetAssignments(It.IsAny<Guid>()) == assignmentsList);

            var authorizedUser = new Mock<IAuthorizedUser>();

            var controller = new AssignmentsApiV2Controller(authorizedUser.Object, assignmentService,
                Mock.Of<IUserToDeviceService>(),
                Mock.Of<ICommandService>());


            controller.ControllerContext = new ControllerContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()));
            controller.Request.Headers[HeaderNames.UserAgent] = "org.worldbank.solutions.interviewer/25.01 (build 33333) (QuestionnaireVersion/27.0.0)";

            var assignments =  controller.GetAssignments(new CancellationToken()).Value;

            Assert.That(assignments.Single(), Has.Property(nameof(AssignmentApiView.Quantity))
                .EqualTo(10 /* assignment.Quantity */ - 5 /* interviewSummary.Count */));
        }

        [Test]
        public void should_return_assignment_accounting_created_interviews()
        {
            var assignmentEntity = Create.Entity.Assignment(quantity: 10, interviewSummary: new HashSet<InterviewSummary>
            {
                Create.Entity.InterviewSummary(status: InterviewStatus.Completed),
                Create.Entity.InterviewSummary(status: InterviewStatus.Completed),

                Create.Entity.InterviewSummary(status: InterviewStatus.InterviewerAssigned),

                Create.Entity.InterviewSummary(status: InterviewStatus.RejectedByHeadquarters),
                Create.Entity.InterviewSummary(status: InterviewStatus.RejectedBySupervisor)
            });
            
            var assignmentServiceImpl = new AssignmentsService(null, null, null, null);

            var assignment = assignmentServiceImpl.MapAssignment(assignmentEntity);
            
            Assert.That(assignment, Has.Property(nameof(AssignmentApiView.Quantity))
                .EqualTo(10 /* assignment.Quantity */ - 5 /* interviewSummary.Count */));
        }
    }
}
