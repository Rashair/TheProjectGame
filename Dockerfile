FROM microsoft/dotnet:2.1-sdk
# install nodejs for building & testing frontend
RUN curl -sL https://deb.nodesource.com/setup_10.x  | bash -
RUN apt-get -y install nodejs
RUN node -v
CMD echo fs.inotify.max_user_instances=524288 | tee -a /etc/sysctl.conf && sysctl -p
