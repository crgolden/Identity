CREATE TABLE [dbo].[SamlLogoutSessions] (
    [Id]                BIGINT         IDENTITY (1, 1) NOT NULL,
    [LogoutId]          NVARCHAR (200) NOT NULL,
    [SerializedSession] NVARCHAR (MAX) NOT NULL,
    [ExpiresAtUtc]      DATETIME2 (7)  NOT NULL,
    [Version]           BIGINT         NOT NULL,
    CONSTRAINT [PK_SamlLogoutSessions] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_SamlLogoutSessions_ExpiresAtUtc]
    ON [dbo].[SamlLogoutSessions]([ExpiresAtUtc] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlLogoutSessions_LogoutId]
    ON [dbo].[SamlLogoutSessions]([LogoutId] ASC);

