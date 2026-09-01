namespace Identity.CAPTCHA;

#pragma warning disable S101
public sealed record CAPTCHAVerdict(bool Passed, decimal Score, bool MonitorOnly);
#pragma warning restore S101
