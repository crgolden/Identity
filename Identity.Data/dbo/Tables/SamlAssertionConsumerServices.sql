CREATE TABLE [dbo].[SamlAssertionConsumerServices] (
    [Id]                    INT            IDENTITY (1, 1) NOT NULL,
    [Location]              NVARCHAR (400) NOT NULL,
    [Binding]               NVARCHAR (200) NOT NULL,
    [Index]                 INT            NOT NULL,
    [IsDefault]             BIT            NOT NULL,
    [SamlServiceProviderId] INT            NOT NULL,
    CONSTRAINT [PK_SamlAssertionConsumerServices] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SamlAssertionConsumerServices_SamlServiceProviders_SamlServiceProviderId] FOREIGN KEY ([SamlServiceProviderId]) REFERENCES [dbo].[SamlServiceProviders] ([Id]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SamlAssertionConsumerServices_SamlServiceProviderId_Location]
    ON [dbo].[SamlAssertionConsumerServices]([SamlServiceProviderId] ASC, [Location] ASC);

