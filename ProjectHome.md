A simple IHttpModule to provide a reverse proxy via IIS. It's written in C#, using Visual Studio 2008.

Reverse proxies are often used to provide public access to a backend sever, or to provide a single public URL to a range of servers even if they are running on multiple platforms.

In my case, I've written it provide access to a web-service that would normally only be available via a private WAN. Authentication and encryption is provided via IIS's X.509 PKI implimentation.