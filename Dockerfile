FROM microsoft/dotnet:2.1.805-sdk
# install nodejs for building & testing frontend
RUN curl -sL https://deb.nodesource.com/setup_10.x  | bash -
RUN apt-get -y install nodejs
RUN node -v