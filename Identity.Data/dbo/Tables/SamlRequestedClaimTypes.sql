CREATE TABLE [dbo].[SamlRequestedClaimTypes] (
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [ClaimType]             NVARCHAR (250) NOT NULL,
    [SamlServiceProviderId] INT            NOT NULL,
    CONSTRAINT [PK_SamlRequestedClaimTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SamlRequestedClaimTypes_SamlServiceProviders_SamlServiceProviderId] FOREIGN KEY ([SamlServiceProviderId]) REFERENCES [dbo].[SamlServiceProviders] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlRequestedClaimTypes_SamlServiceProviderId_ClaimType]
    ON [dbo].[SamlRequestedClaimTypes]([SamlServiceProviderId] ASC, [ClaimType] ASC);

