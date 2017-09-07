﻿using System.Web.Mvc;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Filters;

namespace WB.UI.Headquarters.Controllers
{
    [Authorize(Roles = "Administrator, Headquarter, Supervisor, Observer")]
    [LimitsFilter]
    [ActivePage(MenuItem.Assignments)]
    public class AssignmentsController : BaseController
    {
        private readonly IAuthorizedUser currentUser;

        public AssignmentsController(ICommandService commandService,
            ILogger logger,
            IAuthorizedUser currentUser)
            : base(commandService, logger)
        {
            this.currentUser = currentUser;
        }

        public ActionResult Index()
        {
            var model = new AssignmentsFilters();
            model.IsSupervisor = this.currentUser.IsSupervisor;
            model.IsHeadquarter = this.currentUser.IsHeadquarter || this.currentUser.IsAdministrator;

            return View(model);
        }
    }

    public class AssignmentsFilters
    {
        public bool IsHeadquarter { get; set; }
        public bool IsSupervisor { get; set; }
    }
}