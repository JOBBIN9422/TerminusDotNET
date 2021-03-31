#!/bin/bash
set -euo pipefail
# This is a script meant to pull in the latest echo bot and run it
# Expects the ECHOBOT_DIR variable to be exported before running

# If no branch name is exported default to master
: "${ECHOBOT_BRANCH:="main"}"

# If no dir is specified, fail
if [ -z "${ECHOBOT_DIR}" ]; then
    exit 1
fi

# Working directory should be the echobot dir
cd $ECHOBOT_DIR

# Check if echobot repo is already here
if [ ! -d "Echo-Bot" ]; then
    # Need to clone the repo
    git clone https://github.com/leilanihc112/Echo-Bot.git
fi

# Checkout the selected branch and pull latest
# I hope this won't ever fail, but I guess I can fix it if it does
cd Echo-Bot
git fetch origin $ECHOBOT_BRANCH
git checkout $ECHOBOT_BRANCH
git pull origin $ECHOBOT_BRANCH

# Copy over files into echobot dir
rsync -r ../persistent_files/ ./echobot/

# Run Echo bot
./run.sh