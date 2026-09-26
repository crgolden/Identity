namespace Identity.CAPTCHA;

public sealed record CAPTCHAVerdict(bool Passed, decimal Score);
