namespace Identity.Tests.Unit.PropertyBased;

internal static class PasswordFixtureConstants
{
    internal const int MinGeneratedPasswordLength = 1;
    internal const int MaxGeneratedPasswordLength = 72;
    internal const char WrongPasswordSuffix = 'X';
    internal const int LatinSupplementFirstCodePoint = 0x00C0;
    internal const int LatinSupplementLastCodePoint = 0x00FF;
    internal const int GreekFirstCodePoint = 0x0391;
    internal const int GreekLastCodePoint = 0x03C9;
    internal const int CyrillicFirstCodePoint = 0x0410;
    internal const int CyrillicLastCodePoint = 0x044F;
    internal const int ArabicFirstCodePoint = 0x0627;
    internal const int ArabicLastCodePoint = 0x064A;
    internal const int HiraganaFirstCodePoint = 0x3041;
    internal const int KatakanaLastCodePoint = 0x30FA;
    internal const int CjkFirstCodePoint = 0x4E00;
    internal const int CjkLastCodePoint = 0x9FA5;
    internal const int HangulFirstCodePoint = 0xAC00;
    internal const int HangulLastCodePoint = 0xD7A3;
    internal const int EmojiFirstCodePoint = 0x1F300;
    internal const int EmojiLastCodePoint = 0x1F5FF;
}
