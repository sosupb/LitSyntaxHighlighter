using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Tagger.TagDefinitions
{
    internal static class LitTemplateAttributeNameClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.AttributeNameDark)]
        internal static ClassificationTypeDefinition AttributeNameDark = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(LitClassificationNames.AttributeNameLight)]
        internal static ClassificationTypeDefinition AttributeNameLight = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.AttributeNameDark)]
    [Name(LitClassificationNames.AttributeNameDark)]
    [UserVisible(true)]
    internal class LitTemplateAttributeNameDarkFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateAttributeNameDarkFormatDefinition()
        {
            this.DisplayName = "Lit Attribute Name Dark Character (String Literal)";
            this.ForegroundColor = Color.FromRgb(156, 220, 254); ;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = LitClassificationNames.AttributeNameLight)]
    [Name(LitClassificationNames.AttributeNameLight)]
    [UserVisible(true)]
    internal class LitTemplateAttributeNameLightFormatDefinition : ClassificationFormatDefinition
    {
        public LitTemplateAttributeNameLightFormatDefinition()
        {
            this.DisplayName = "Lit Attribute Name Light Character (String Literal)";
            this.ForegroundColor = Colors.Red;
        }
    }
}
