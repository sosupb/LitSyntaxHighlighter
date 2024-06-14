using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Tagger.TagDefinitions
{
    internal static class LitTemplateElementClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.ElementDark)]
        internal static ClassificationTypeDefinition ElementDark = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.ElementLight)]
        internal static ClassificationTypeDefinition ElementLight = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.ElementDark)]
    [Name(LitClassificationNames.ElementDark)]
    [UserVisible(true)]
    internal class LitElementDarkFormatDefinition : ClassificationFormatDefinition
    {

        public LitElementDarkFormatDefinition()
        {
            this.DisplayName = "Lit Element Dark (String Literal)";
            this.ForegroundColor = Color.FromRgb(86, 156, 214);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.ElementDark)]
    [Name(LitClassificationNames.ElementLight)]
    [UserVisible(true)]
    internal class LitElementLightFormatDefinition : ClassificationFormatDefinition
    {

        public LitElementLightFormatDefinition()
        {
            this.DisplayName = "Lit Element Light (String Literal)";
            this.ForegroundColor = Color.FromRgb(128, 0, 0);
        }
    }
}
