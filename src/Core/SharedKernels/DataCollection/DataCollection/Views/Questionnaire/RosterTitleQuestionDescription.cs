﻿using System;
using System.Collections.Generic;

namespace WB.Core.SharedKernels.DataCollection.Views.Questionnaire
{
    public class RosterTitleQuestionDescription
    {
        public RosterTitleQuestionDescription(Guid questionId)
        {
            this.QuestionId = questionId;
        }

        public Guid QuestionId { get; private set; }
    }
}
