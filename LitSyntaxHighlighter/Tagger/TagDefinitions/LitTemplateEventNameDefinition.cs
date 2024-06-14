using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Tagger.TagDefinitions
{
    internal static class LitTemplateEventNameClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.EventNameDark)]
        internal static ClassificationTypeDefinition EventNameDark = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.EventNameLight)]
        internal static ClassificationTypeDefinition EventNameLight = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.EventNameDark)]
    [Name(LitClassificationNames.EventNameDark)]
    [UserVisible(true)]
    internal class LitTemplateEventNameDarkFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateEventNameDarkFormatDefinition()
        {
            this.DisplayName = "Lit Event Name Dark Character (String Literal)";
            this.ForegroundColor = Color.FromRgb(220, 220, 170);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.EventNameLight)]
    [Name(LitClassificationNames.EventNameLight)]
    [UserVisible(true)]
    internal class LitTemplateEventNameLightFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateEventNameLightFormatDefinition()
        {
            this.DisplayName = "Lit Event Name Light Character (String Literal)";
            this.ForegroundColor = Color.FromRgb(116, 83, 31);
        }
    }
}
