CREATE TABLE [dbo].[SamlCertificates] (
    [Id]                    INT             IDENTITY (1, 1) NOT NULL,
    [Data]                  NVARCHAR (4000) NOT NULL,
    [Use]                   INT             NOT NULL,
    [SamlServiceProviderId] INT             NOT NULL,
    CONSTRAINT [PK_SamlCertificates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SamlCertificates_SamlServiceProviders_SamlServiceProviderId] FOREIGN KEY ([SamlServiceProviderId]) REFERENCES [dbo].[SamlServiceProviders] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_SamlCertificates_SamlServiceProviderId]
    ON [dbo].[SamlCertificates]([SamlServiceProviderId] ASC);

