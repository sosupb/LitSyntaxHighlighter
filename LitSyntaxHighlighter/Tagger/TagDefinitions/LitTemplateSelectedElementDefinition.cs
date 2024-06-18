using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Tagger.TagDefinitions
{
    internal static class LitTemplateSelectedElementNameClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.SelectedElementNameDark)]
        internal static ClassificationTypeDefinition SelectedElementNameDark = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.SelectedElementNameLight)]
        internal static ClassificationTypeDefinition SelectedElementNameLight = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.SelectedElementNameDark)]
    [Name(LitClassificationNames.SelectedElementNameDark)]
    [UserVisible(true)]
    internal class LitTemplateSelectedElementNameDarkFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateSelectedElementNameDarkFormatDefinition()
        {
            this.DisplayName = "Lit Selected Element Name Dark Character (String Literal)";
            this.ForegroundColor = Color.FromRgb(86, 156, 214);
            this.BackgroundColor = Color.FromRgb(33, 89, 135);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.SelectedElementNameLight)]
    [Name(LitClassificationNames.SelectedElementNameLight)]
    [UserVisible(true)]
    internal class LitTemplateSelectedElementNameLightFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateSelectedElementNameLightFormatDefinition()
        {
            this.DisplayName = "Lit Selected Element Name Light Character (String Literal)";
            this.ForegroundColor = Color.FromRgb(128, 0, 0);
            this.BackgroundColor = Color.FromRgb(185, 214, 238);
        }
    }
}
