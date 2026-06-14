CREATE TABLE [dbo].[SamlAllowedScopes] (
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [Scope]                 NVARCHAR (200) NOT NULL,
    [SamlServiceProviderId] INT            NOT NULL,
    CONSTRAINT [PK_SamlAllowedScopes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SamlAllowedScopes_SamlServiceProviders_SamlServiceProviderId] FOREIGN KEY ([SamlServiceProviderId]) REFERENCES [dbo].[SamlServiceProviders] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlAllowedScopes_SamlServiceProviderId_Scope]
    ON [dbo].[SamlAllowedScopes]([SamlServiceProviderId] ASC, [Scope] ASC);

