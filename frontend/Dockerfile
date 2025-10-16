FROM node:18-alpine AS build
WORKDIR /web
COPY ./frontend/ ./
ARG VITE_API_BASE=http://localhost:8080
RUN echo "VITE_API_BASE=$VITE_API_BASE" > .env.production
RUN npm ci || npm i
RUN npm run build

FROM nginx:alpine AS serve
COPY --from=build /web/dist/ /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
