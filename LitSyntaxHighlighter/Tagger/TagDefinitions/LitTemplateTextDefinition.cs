using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Tagger.TagDefinitions
{
    internal static class LitTemplateTextClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.TextDark)]
        internal static ClassificationTypeDefinition TextDark = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.TextLight)]
        internal static ClassificationTypeDefinition TextLight = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.TextDark)]
    [Name(LitClassificationNames.TextDark)]
    [UserVisible(true)]
    internal class LitTemplateTextDarkFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateTextDarkFormatDefinition()
        {
            this.DisplayName = "Lit Text Dark Character (String Literal)";
            this.ForegroundColor = Color.FromRgb(210, 210, 210);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.TextLight)]
    [Name(LitClassificationNames.TextLight)]
    [UserVisible(true)]
    internal class LitTemplateTextLightFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateTextLightFormatDefinition()
        {
            this.DisplayName = "Lit Text Light Character (String Literal)";
            this.ForegroundColor = Colors.Black;
        }
    }
}
