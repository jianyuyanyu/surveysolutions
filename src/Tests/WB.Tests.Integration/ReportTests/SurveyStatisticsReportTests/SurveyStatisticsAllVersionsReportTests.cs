﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities.Question;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.SurveyStatistics;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.SurveyStatistics.Data;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Tests.Abc;
using WB.Tests.Integration.InterviewFactoryTests;

namespace WB.Tests.Integration.ReportTests.SurveyStatisticsReportTests
{
    internal class SurveyStatisticsAllVersionsReportTests : InterviewFactorySpecification
    {
        private QuestionnaireDocument questionnaireV1;
        private QuestionnaireDocument questionnaireV2;

        Dictionary<int, QuestionnaireDocument> Questionnaires;

        private readonly Guid relationQuestion = Id.g1;
        private readonly Guid sexQuestion = Id.g2;
        private readonly Guid dwellingQuestion = Id.g3;
        private Guid questionnaireId;

        private InterviewFactory factory;
        private SurveyStatisticsReport reporter;
        private const string teamLeadName = "teamLead";

        internal enum Relation { Head = 1, Spouse, Child }
        internal enum Sex { Male = 1, Female, Human }
        internal enum Dwelling { House = 1, Barrack, Hole, Moon }

        [SetUp]
        public void SettingUp()
        {
            Questionnaires = new Dictionary<int, QuestionnaireDocument>();
            questionnaireId = Guid.NewGuid();

            this.questionnaireV1 = Create.Entity.QuestionnaireDocumentWithOneChapter(null, questionnaireId,
                Create.Entity.SingleOptionQuestion(dwellingQuestion, "dwelling", answers: GetAnswersFromEnum<Dwelling>()),
                Create.Entity.Roster(Id.gA, variable: "hh_member", children: new[]
                {
                    Create.Entity.SingleOptionQuestion(relationQuestion, variable: "relation", answers: GetAnswersFromEnum<Relation>()),
                    Create.Entity.SingleOptionQuestion(sexQuestion,      variable: "sex",      answers: GetAnswersFromEnum<Sex>())
                })
            );

            PrepareQuestionnaire(questionnaireV1, 1);
            Questionnaires.Add(1, questionnaireV1);

            this.questionnaireV2 = Create.Entity.QuestionnaireDocumentWithOneChapter(null, questionnaireId,
                Create.Entity.SingleOptionQuestion(dwellingQuestion, "dwelling", answers: GetAnswersFromEnum<Dwelling>()),
                Create.Entity.Roster(Id.gA, variable: "hh_member", children: new[]
                {
                    Create.Entity.SingleOptionQuestion(relationQuestion, variable: "relation", answers: GetAnswersFromEnum<Relation>()),
                    Create.Entity.SingleOptionQuestion(sexQuestion,      variable: "sex",      answers: GetAnswersFromEnum<Sex>(exclude: Sex.Human))
                })
            );

            PrepareQuestionnaire(questionnaireV2, 2);
            Questionnaires.Add(2, questionnaireV2);

            this.factory = CreateInterviewFactory();

            Because();

            this.reporter = new SurveyStatisticsReport(new InterviewReportDataRepository(UnitOfWork));
        }

        private void Because()
        {
            // Creating 4 interviews with different members configuration

            CreateInterview(1, Dwelling.House,
                (Relation.Head, Sex.Male),
                (Relation.Spouse, Sex.Female));

            CreateInterview(1, Dwelling.House,
                (Relation.Head, Sex.Human),
                (Relation.Spouse, Sex.Human));

            CreateInterview(1, Dwelling.Barrack,
                (Relation.Head, Sex.Female),
                (Relation.Spouse, Sex.Male));

            CreateInterview(2, Dwelling.Hole,
                (Relation.Head, Sex.Male),
                (Relation.Spouse, Sex.Female),
                (Relation.Child, Sex.Male));

            CreateInterview(2, Dwelling.House,
                (Relation.Head, Sex.Female));

            // there is in total 8 members in survey
            // 4 heads, 3 spouses and 1 child
            // 4 male and 4 female members
            //  3 people live in houses, 2 ion barracks and 3 in a hole in the ground
        }

        [Test]
        public void Categorical_report_by_sex_should_has_warning()
        {
            var question = this.questionnaireV2.Find<SingleQuestion>(sexQuestion);

            var report = this.reporter.GetReport(new SurveyStatisticsReportInputModel
            {
                QuestionnaireId = questionnaireV2.PublicKey.FormatGuid(),
                Question = question
            });

            // there is 4 males ,4 females in total 8
            Assert.That(report.Data[0], Is.EqualTo(new object[] { teamLeadName, null, 4, 4, 8 }));
            Assert.That(report.Warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void Categorical_report_by_sex_with_condition_by_relation_should_has_warning()
        {
            var question = this.questionnaireV2.Find<SingleQuestion>(sexQuestion);

            var report = this.reporter.GetReport(new SurveyStatisticsReportInputModel
            {
                QuestionnaireId = questionnaireV2.PublicKey.FormatGuid(),
                ConditionalQuestion = this.questionnaireV2.Find<SingleQuestion>(relationQuestion),
                Condition = new[] { (long)Relation.Spouse },
                Question = question
            });

            // there is 1 male and 2 female spouses, in total there is 3 spouses
            Assert.That(report.Data[0], Is.EqualTo(new object[] { teamLeadName, null, 1, 2, 3 }));
            Assert.That(report.Warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void PivotReport_report_proper_data_should_has_warning()
        {
            var report = this.reporter.GetReport(new SurveyStatisticsReportInputModel
            {
                QuestionnaireId = questionnaireV2.PublicKey.FormatGuid(),
                ConditionalQuestion = this.questionnaireV2.Find<SingleQuestion>(relationQuestion),
                Question = this.questionnaireV2.Find<SingleQuestion>(sexQuestion),
                Pivot = true
            });

            // there is 2 male and 2 female head members
            Assert.That(report.Data[0], Is.EqualTo(new object[] { Relation.Head.ToString(), 2, 2, 4 }));

            // there is 1 male and 2 female spouse members
            Assert.That(report.Data[1], Is.EqualTo(new object[] { Relation.Spouse.ToString(), 1, 2, 3 }));

            // there is 1 male and 0 female child members
            Assert.That(report.Data[2], Is.EqualTo(new object[] { Relation.Child.ToString(), 1, 0, 1 }));

            Assert.That(report.Warnings, Has.Count.EqualTo(1));
        }

        //                                                            object[] { Male, Female, Total }
        [TestCase(Dwelling.House, ExpectedResult = new object[] { 1, 2, 3 })]
        [TestCase(Dwelling.Hole, Dwelling.House, ExpectedResult = new object[] { 3, 3, 6 })]
        [TestCase(Dwelling.Barrack, ExpectedResult = new object[] { 1, 1, 2 })]
        [TestCase(Dwelling.Barrack, Dwelling.Hole, Dwelling.House, Description = "Should return all members",
            ExpectedResult = new object[] { 4, 4, 8 })]
        public object[] Should_be_able_to_condition_report_by_non_roster_variable(params Dwelling[] condition)
        {
            var report = this.reporter.GetReport(new SurveyStatisticsReportInputModel
            {
                QuestionnaireId = questionnaireV2.PublicKey.FormatGuid(),
                ConditionalQuestion = this.questionnaireV2.Find<SingleQuestion>(dwellingQuestion),
                Question = this.questionnaireV2.Find<SingleQuestion>(sexQuestion),
                Condition = condition.Select(c => (long)c).ToArray()
            });
            
            return report.Data[0].Skip(2).ToArray();
        }

        private void CreateInterview(int version, Dwelling dwelling, params (Relation rel, Sex sex)[] members)
        {
            var interviewId = Guid.NewGuid();
            var questionnaire = Questionnaires[version];

            StoreInterviewSummary(new InterviewSummary(questionnaire)
            {
                InterviewId = interviewId,
                Status = InterviewStatus.Completed,
                ResponsibleName = "responsible",
                ResponsibleId = Id.gC,
                TeamLeadId = Id.gE,
                TeamLeadName = teamLeadName
            }, new QuestionnaireIdentity(questionnaire.PublicKey, version));

            var state = Create.Entity.InterviewState(interviewId);

            SetIntAnswer(dwellingQuestion, (int)dwelling);

            for (var vector = 0; vector < members.Length; vector++)
            {
                var member = members[vector];

                SetIntAnswer(relationQuestion, (int)member.rel, vector);
                SetIntAnswer(sexQuestion, (int)member.sex, vector);
            }

            factory.Save(state);

            void SetIntAnswer(Guid questionId, int answer, params int[] rosterVector)
            {
                var question = InterviewStateIdentity.Create(questionId, rosterVector);
                state.Enablement[question] = true;
                state.Answers[question] = new InterviewStateAnswer { AsInt = answer };
            }
        }
    }
}