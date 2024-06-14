using LitSyntaxHighlighter.Tagger.TagDefinitions;
using LitSyntaxHighlighter.Utility;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace LitSyntaxHighlighter.Tagger
{
    internal enum TagType
    {
        Delimiter,
        Element,
        AttributeName,
        EventName,
        Text,
        Comment
    }

    internal class LitTemplateTagRegistry
    {
        
        public IDictionary<TagType, ClassificationTag> ClassificationTags 
        { 
            get
            {
                return ThemeUtility.IsLightTheme ? _lightThemeTags : _darkThemeTags;
            }
        }

        private IDictionary<TagType, ClassificationTag> _darkThemeTags;
        private IDictionary<TagType, ClassificationTag> _lightThemeTags;

        public LitTemplateTagRegistry(IClassificationTypeRegistryService registry)
        {
            _lightThemeTags = new Dictionary<TagType, ClassificationTag>()
            {
                { TagType.Delimiter, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterLight)) },
                { TagType.Comment, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.CommentLight)) },
                { TagType.Element, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementLight)) },
                { TagType.AttributeName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.AttributeNameLight)) },
                { TagType.EventName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.EventNameLight)) },
                { TagType.Text, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.TextLight)) }
            };
            _darkThemeTags = new Dictionary<TagType, ClassificationTag>()
            {
                { TagType.Delimiter, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterDark)) },
                { TagType.Comment, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.CommentDark)) },
                { TagType.Element, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementDark)) },
                { TagType.AttributeName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.AttributeNameDark)) },
                { TagType.EventName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.EventNameDark)) },
                { TagType.Text, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.TextDark)) }
            };
        }
    }
}
