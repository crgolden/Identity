CREATE TABLE [dbo].[SamlAuthnContextMappings] (
    [Id]                      INT            IDENTITY (1, 1) NOT NULL,
    [OidcValue]               NVARCHAR (250) NOT NULL,
    [SamlAuthnContextClassRef] NVARCHAR (500) NOT NULL,
    [SamlServiceProviderId]   INT            NOT NULL,
    CONSTRAINT [PK_SamlAuthnContextMappings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SamlAuthnContextMappings_SamlServiceProviders_SamlServiceProviderId] FOREIGN KEY ([SamlServiceProviderId]) REFERENCES [dbo].[SamlServiceProviders] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlAuthnContextMappings_SamlServiceProviderId_OidcValue]
    ON [dbo].[SamlAuthnContextMappings]([SamlServiceProviderId] ASC, [OidcValue] ASC);

