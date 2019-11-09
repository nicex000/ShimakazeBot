FROM mcr.microsoft.com/dotnet/core/runtime:2.2-alpine

RUN apk update
RUN apk add openjdk8

WORKDIR /root

COPY * Shima/

WORKDIR Shima

RUN chmod +x start.sh

CMD ["./start.sh"]
