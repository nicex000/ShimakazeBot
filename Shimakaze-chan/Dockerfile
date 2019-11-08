FROM mcr.microsoft.com/dotnet/core/runtime:2.2-alpine

RUN apk update
RUN apk add openjdk8 postgresql

##### POSTGRES SETUP #####

ENV PGDATA /var/lib/postgresql/data

RUN mkdir -p /var/run/postgresql && chown -R postgres:postgres /var/run/postgresql && chmod 2777 /var/run/postgresql &&\
    mkdir -p "$PGDATA" && chown -R postgres:postgres "$PGDATA" && chmod 777 "$PGDATA"

USER postgres

RUN initdb "$PGDATA"&&\
    echo "host all  all    0.0.0.0/0  md5" >> /var/lib/postgresql/data/pg_hba.conf &&\
    pg_ctl start &&\
    psql --command "ALTER USER postgres WITH ENCRYPTED PASSWORD 'password';"

##### POSTGRES SETUP END #####

USER root

WORKDIR /root

COPY * Shima/

WORKDIR Shima

RUN chmod +x start.sh

CMD ["./start.sh"]
