﻿#nullable enable

using System;
using System.Collections.Generic;

namespace WB.Core.BoundedContexts.Designer.ImportExport.Models
{
    public class Questionnaire
    {
        public Guid Id { get; set; }
        public CoverPage? CoverPage { get; set; }
        public List<Group> Children { get; set; } = new List<Group>();
        public List<Macro> Macros { get; set; } = new List<Macro>();
        public List<CriticalRule> CriticalRules { get; set; } = new List<CriticalRule>();
        public List<LookupTable> LookupTables { get; set; } = new List<LookupTable>();
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public Translations Translations { get; set; } = new Translations();
        public List<Categories> Categories { get; set; } = new List<Categories>();
        public bool HideIfDisabled { get; set; }
        public string? Title { get; set; }
        public string Description { get; set; } = String.Empty;
        public QuestionnaireMetaInfo? Metadata { get; set; }
        public string VariableName { get; set; } = String.Empty;
    }
}
