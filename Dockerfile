FROM microsoft/dotnet:2.1-sdk
# install nodejs for building & testing frontend
RUN curl -sL https://deb.nodesource.com/setup_10.x  | bash -
RUN apt-get -y install nodejs
RUN node -v
RUN echo fs.inotify.max_user_instances=524288 | sudo tee -a /etc/sysctl.conf && sudo sysctl -p