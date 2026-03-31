FROM node:22-slim AS client
EXPOSE 4200
USER node

RUN echo "Configuring NPM..."
RUN mkdir /home/node/.npm-global
ENV PATH=/home/node/.npm-global/bin:$PATH
ENV NPM_CONFIG_PREFIX=/home/node/.npm-global

RUN echo "Installing angular..."
RUN npm install -g @angular/cli@21

RUN echo "Serving app..."
WORKDIR /app
CMD ["sh", "-c", "npm install && ng serve --port 4200 --configuration development --host 0.0.0.0 --poll 2000"]

#"--ssl", "--ssl-cert", "%APPDATA%\\ASP.NET\\https\\client-app.pem", "--ssl-key", "%APPDATA%\\ASP.NET\\https\\client-app.key"
