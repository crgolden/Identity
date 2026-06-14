CREATE TABLE [dbo].[SamlLogoutSessionRequestIndices] (
    [Id]                   BIGINT         IDENTITY (1, 1) NOT NULL,
    [RequestId]            NVARCHAR (200) NOT NULL,
    [SamlLogoutSessionId]  BIGINT         NOT NULL,
    CONSTRAINT [PK_SamlLogoutSessionRequestIndices] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SamlLogoutSessionRequestIndices_SamlLogoutSessions_SamlLogoutSessionId] FOREIGN KEY ([SamlLogoutSessionId]) REFERENCES [dbo].[SamlLogoutSessions] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlLogoutSessionRequestIndices_RequestId]
    ON [dbo].[SamlLogoutSessionRequestIndices]([RequestId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_SamlLogoutSessionRequestIndices_SamlLogoutSessionId]
    ON [dbo].[SamlLogoutSessionRequestIndices]([SamlLogoutSessionId] ASC);

