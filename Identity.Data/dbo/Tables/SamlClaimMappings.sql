CREATE TABLE [dbo].[SamlClaimMappings] (
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [ClaimType]             NVARCHAR (250) NOT NULL,
    [SamlAttributeName]     NVARCHAR (250) NOT NULL,
    [SamlServiceProviderId] INT            NOT NULL,
    CONSTRAINT [PK_SamlClaimMappings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SamlClaimMappings_SamlServiceProviders_SamlServiceProviderId] FOREIGN KEY ([SamlServiceProviderId]) REFERENCES [dbo].[SamlServiceProviders] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlClaimMappings_SamlServiceProviderId_ClaimType]
    ON [dbo].[SamlClaimMappings]([SamlServiceProviderId] ASC, [ClaimType] ASC);

