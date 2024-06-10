using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace LitSyntaxHighlighter
{
    internal static class FormatNames
    {
        public const string Delimiter = "LitDelimiter";
        public const string Element = "LitElement";
        public const string AttributeName = "LitAttributeName";
        public const string AttributeDelimiter = "LitAttributeDelimiter";
        public const string EventName = "LitEventName";
        public const string Quote = "LitQuote";
        public const string AttributeValue = "LitAttributeValue";
        public const string Text = "LitText";
        public const string Comment = "LitComment";
    }

    /// <summary>
    /// Classification type definition export for LitClassifier
    /// </summary>
    internal static class LitClassifierClassificationDefinition
    {
        // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Delimiter)]
        internal static ClassificationTypeDefinition Delimiter = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Element)]
        internal static ClassificationTypeDefinition Element = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.AttributeName)]
        internal static ClassificationTypeDefinition AttributeName = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.AttributeDelimiter)]
        internal static ClassificationTypeDefinition AttributeDelimiter = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.EventName)]
        internal static ClassificationTypeDefinition EventName = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Quote)]
        internal static ClassificationTypeDefinition Quote = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.AttributeValue)]
        internal static ClassificationTypeDefinition AttributeValue = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Text)]
        internal static ClassificationTypeDefinition Text = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(FormatNames.Comment)]
        internal static ClassificationTypeDefinition Comment = null;

#pragma warning restore 169
    }
}
