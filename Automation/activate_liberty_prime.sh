#!/bin/bash
# This is a script meant to pull in the latest liberty prime and run it
# Expects the LIBERTY_DIR variable to be exported before running

# If no branch name is exported defualt to master
: "${LIBERTY_BRANCH:="master"}"

# If no dir is specified, fail
if [ -z "${LIBERTY_DIR}" ]; then
    exit 1
fi

# Working directory should be the liberty dir
cd $LIBERTY_DIR

# Check if liberty prime repo is already here
if [ ! -d "liberty" ]; then
    # Need to clone the repo
    git clone https://github.com/MichaelHHall/liberty.git
fi

# Checkout the selected branch and pull latest
# I hope this won't ever fail, but I guess I can fix it if it does
cd liberty
git checkout $LIBERTY_BRANCH
git pull origin $LIBERTY_BRANCH

# Copy over files into liberty dir
rsync -r persistent_files/ liberty/

# Run Liberty
./run.sh
