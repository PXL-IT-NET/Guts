FROM node:8 AS node-builder
WORKDIR /src
COPY . .
RUN npm run webpack

FROM microsoft/dotnet:2.1-sdk
COPY . /app
WORKDIR /app
COPY --from=node-builder /src/dist/*.js  ./dist
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080
ENTRYPOINT ["dotnet", "Guts.Web.dll"]
