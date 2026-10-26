FROM node:24-slim AS client
EXPOSE 4200
USER node

ENV PATH=/home/node/.npm-global/bin:$PATH \
    NPM_CONFIG_PREFIX=/home/node/.npm-global

RUN mkdir -p /home/node/.npm-global && \
    npm install -g --ignore-scripts @angular/cli@22.2.3

WORKDIR /app
CMD ["sh", "-c", "npm install && ng serve --port 4200 --configuration development --host 0.0.0.0 --poll 2000"]