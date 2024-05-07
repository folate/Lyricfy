# Lyricfy
A simple web application for sharing song lyrics as images (similar to what Spotify's mobile app can do) made with ASP.NET Blazor WASM and ASP.NET Core Web API

## Why
I made this because there wasn't any way to share lyrics on desktop like you can do in spotify mobile app.

## Whats what
This solution has 3 seperate projets:
* Cors - cors proxy required to scrape specific websites
* Lyricfy - the web app itself
* LyricfyLibraries - library used in Lyricfy to simplify code in Lyricfy project, fetches and parses lyrics and metadata without using spoify's and genius's APIs

## Before setup
To run this project you **need** to install **docker** as well as **nginx** (you can run this inside a container too). Also there could be issues with genius blocking ip on which cors is running as it detects if the ip is a hosting one.

## How to setup
I have attached a sample docker compose file (in the root folder of this repository) and dockerfiles for each project (except LyricfyLibraries; each dockerfile is in each project folder). 

After running `docker compose up`, the next step would be to change the urls in `appsettings.json` which is found in the Lyricfy project in the `wwwroot` folder to the valid ones (just replace `localhost:5217` with `yourdomain.com` or the ip addresses where Cors is running).

Then you need to configure nginx to proxy pass requests to the correct containers if you're running this on the one server where you have other web applications (which I can't do because my server's IP address is marked as hosted and therefore genius blocks it; I, for example, set nginx to listen for all requests coming from `lyricfy. folate.fun` to proxy pass everything to my first server with the Lyricfy blazor wasm application container, but `lyricfyapi.folate.fun` points to another server with Cors).

I don't want to go into the details of how to get this working, because it's easy to figure out, and the project itself is unpolished and unfinished.
