using LitSyntaxHighlighter.Tagger.TagDefinitions;
using LitSyntaxHighlighter.Utility;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace LitSyntaxHighlighter.Tagger
{
    [Flags]
    internal enum TagType
    {
        None = 0,
        Delimiter = 1 << 0,
        Element = 1 << 1,
        SelfCloseElement = 1 << 2,
        CloseElement = 1 << 3,
        AttributeName = 1 << 4,
        EventName = 1 << 5,
        Text = 1 << 6,
        CommentStart = 1 << 7,
        Comment = 1 << 8,
        CommentEnd = 1 << 9,
        SelectedOpenElement = 1 << 10,
        SelectedSelfCloseElement = 1 << 11,
        SelectedCloseElement = 1 << 12,
        OpenTags = Element | SelfCloseElement | CommentStart,
        CloseTags = CloseElement | CommentEnd,
        CommentTags = CommentStart | CommentEnd
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

        private bool _isLightTheme;
        private IDictionary<TagType, ClassificationTag> _darkThemeTags;
        private IDictionary<TagType, ClassificationTag> _lightThemeTags;

        public LitTemplateTagRegistry(IClassificationTypeRegistryService registry)
        {
            _lightThemeTags = new Dictionary<TagType, ClassificationTag>()
            {
                { TagType.Delimiter, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterLight)) },
                { TagType.CommentStart, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterLight)) },
                { TagType.Comment, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.CommentLight)) },
                { TagType.CommentEnd, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterLight)) },
                { TagType.Element, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementLight)) },
                { TagType.SelfCloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementLight)) },
                { TagType.CloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementLight)) },
                { TagType.AttributeName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.AttributeNameLight)) },
                { TagType.EventName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.EventNameLight)) },
                { TagType.Text, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.TextLight)) },
                { TagType.SelectedOpenElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.SelectedElementNameLight)) },
                { TagType.SelectedSelfCloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.SelectedElementNameLight)) },
                { TagType.SelectedCloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.SelectedElementNameLight)) }
            };
            _darkThemeTags = new Dictionary<TagType, ClassificationTag>()
            {
                { TagType.Delimiter, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterDark)) },
                { TagType.CommentStart, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterDark)) },
                { TagType.Comment, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.CommentDark)) },
                { TagType.CommentEnd, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.DelimiterDark)) },
                { TagType.Element, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementDark)) },
                { TagType.SelfCloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementDark)) },
                { TagType.CloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.ElementDark)) },
                { TagType.AttributeName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.AttributeNameDark)) },
                { TagType.EventName, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.EventNameDark)) },
                { TagType.Text, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.TextDark)) },
                { TagType.SelectedOpenElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.SelectedElementNameDark)) },
                { TagType.SelectedSelfCloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.SelectedElementNameDark)) },
                { TagType.SelectedCloseElement, new ClassificationTag(registry.GetClassificationType(LitClassificationNames.SelectedElementNameDark)) }
            };
        }
    }
}
