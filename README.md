# Secrets are hidden
[Like this](https://www.codeproject.com/articles/602146/keeping-sensitive-config-settings-secret-with-azur)

*HiddenSettings.config* file  is not checked in to GitHub, it contains the secrets.

To make the Azure deployment succeed, a Post Build event creates an empty *HiddenSettings.config* file.

In Azure Portal, the AppSetting is overriden.

