using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using QuestionnaireVerifier = WB.Core.BoundedContexts.Designer.Verifier.QuestionnaireVerifier;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_question_that_has_substitution_reference_of_not_supported_type : QuestionnaireVerifierTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionWithSubstitutionReferenceToNotSupportedTypeId = Guid.Parse("10000000000000000000000000000000");
            questionSubstitutionReferencerOfNotSupportedTypeId = Guid.Parse("13333333333333333333333333333333");
            questionnaire = CreateQuestionnaireDocument(
                Create.MultyOptionsQuestion(
                questionSubstitutionReferencerOfNotSupportedTypeId,
                variable: unsupported,
                options: new List<Answer> { new Answer() { AnswerValue = "1", AnswerText = "opt 1" }, new Answer() { AnswerValue = "2", AnswerText = "opt 2" } }
            ),
            Create.SingleQuestion(
                questionWithSubstitutionReferenceToNotSupportedTypeId,
                variable: "var",
                title: $"hello %{unsupported}%!",
                options: new List<Answer>() { new Answer() { AnswerValue = "1", AnswerText = "opt 1" }, new Answer() { AnswerValue = "2", AnswerText = "opt 2" } }
            ));

            verifier = CreateQuestionnaireVerifier();
            BecauseOf();
        }

        private void BecauseOf() =>
            verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire));

        [NUnit.Framework.Test] public void should_return_1_message () =>
            verificationMessages.Count().ShouldEqual(1);

        [NUnit.Framework.Test] public void should_return_message_with_code__WB0018 () =>
            verificationMessages.Single().Code.ShouldEqual("WB0018");

        [NUnit.Framework.Test] public void should_return_message_with_2_references () =>
            verificationMessages.Single().References.Count().ShouldEqual(2);

        [NUnit.Framework.Test] public void should_return_firts_message_reference_with_type_Question () =>
            verificationMessages.Single().References.First().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        [NUnit.Framework.Test] public void should_return_firts_message_reference_with_id_of_questionWithNotExistingSubstitutionsId () =>
            verificationMessages.Single().References.First().Id.ShouldEqual(questionWithSubstitutionReferenceToNotSupportedTypeId);

        [NUnit.Framework.Test] public void should_return_last_message_reference_with_type_Question () =>
            verificationMessages.Single().References.Last().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        [NUnit.Framework.Test] public void should_return_last_message_reference_with_id_of_questionSubstitutionReferencerOfNotSupportedTypeId () =>
            verificationMessages.Single().References.Last().Id.ShouldEqual(questionSubstitutionReferencerOfNotSupportedTypeId);

        private static IEnumerable<QuestionnaireVerificationMessage> verificationMessages;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;

        private static Guid questionWithSubstitutionReferenceToNotSupportedTypeId;
        private static Guid questionSubstitutionReferencerOfNotSupportedTypeId;
        private const string unsupported = "unsupported";
    }
}