chat-manager-language-requires-hands = You need free hands to speak this language!

# Same as upstream chat-manager-entity-say(-bold)-wrap-message, plus a color for languages that set one.
ssu-language-say-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]{ chat-manager-speech-double-quote-begin }[BubbleContent][color={$color}]{$message}[/color][/BubbleContent]{ chat-manager-speech-double-quote-end }[/font]
ssu-language-say-bold-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]{ chat-manager-speech-double-quote-begin }[BubbleContent][bold][color={$color}]{$message}[/color][/bold][/BubbleContent]{ chat-manager-speech-double-quote-end }[/font]

# Same as upstream chat-manager-entity-whisper(-unknown)-wrap-message, plus the language's color and font.
ssu-language-whisper-wrap-message = [font size=11][italic][BubbleHeader][Name]{$entityName}[/Name][/BubbleHeader] whispers,{ chat-manager-speech-double-quote-begin }[BubbleContent][color={$color}][font="{$fontType}"]{$message}[/font][/color][/BubbleContent]{ chat-manager-speech-double-quote-end }[/italic][/font]
ssu-language-whisper-unknown-wrap-message = [font size=11][italic][BubbleHeader]Someone[/BubbleHeader] whispers, { chat-manager-speech-double-quote-begin }[BubbleContent][color={$color}][font="{$fontType}"]{$message}[/font][/color][/BubbleContent]{ chat-manager-speech-double-quote-end }[/italic][/font]
