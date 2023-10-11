FROM node:lts-alpine3.18 AS client
EXPOSE 4200
USER node

RUN mkdir /home/node/.npm-global
ENV PATH=/home/node/.npm-global/bin:$PATH
ENV NPM_CONFIG_PREFIX=/home/node/.npm-global

RUN npm install -g @angular/cli@15.2.7
WORKDIR /app
CMD ["ng", "serve", "--port", "4200", "--configuration", "development", "--host", "0.0.0.0", "--disable-host-check", "--poll", "2000"]

#"--ssl", "--ssl-cert", "%APPDATA%\\ASP.NET\\https\\client-app.pem", "--ssl-key", "%APPDATA%\\ASP.NET\\https\\client-app.key"
