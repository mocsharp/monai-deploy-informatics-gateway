# SPDX-FileCopyrightText: © 2022 MONAI Consortium
# SPDX-License-Identifier: Apache License 2.0

#!/bin/bash

# enable(1)/disable(0) VS code attach debuger
export VSTEST_HOST_DEBUG=0

export SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null && pwd)"
TEST_DIR="$SCRIPT_DIR/"
LOG_DIR="${GITHUB_WORKSPACE:-$SCRIPT_DIR}"
RUN_DIR="$SCRIPT_DIR/.run"
BIN_DIR="$TEST_DIR/bin/Release/net6.0"
CONFIG_DIR="$SCRIPT_DIR/configs"
EXIT=false
METRICSFILE="$LOG_DIR/metrics.log"
LOADDEV=
FEATURE=
STREAMID=
export STUDYJSON="study.json"

export DATA_PATH="$RUN_DIR/ig/payloads/"
set -euo pipefail

function info() {
    echo "$(date -u '+%Y-%m-%d %H:%M:%S') [INFO]:" $@
}

function fatal() {
    echo >&2 "$(date -u '+%Y-%m-%d %H:%M:%S') [FATAL]:" $@
    tear_down
    exit 1
}

function check_status_code() {
    STATUS=$1
    info "STATUS=$STATUS"
    if [[ $STATUS == 400 ]] || [[ $STATUS == 500 ]] || [[ $STATUS == 000 ]]; then
        fatal 'HTTP call failed with exit code $STATUS'
    fi
}

function env_setup() {
    [ -d $RUN_DIR ] && info "Removing $RUN_DIR..." && sudo rm -r $RUN_DIR
    mkdir -p $RUN_DIR

    [ -d $BIN_DIR ] && info "Removing $BIN_DIR..." && sudo rm -r $BIN_DIR

    set +u
    if [ "$1" = "--dev" ]; then
        info "Using .env.dev..."
        LOADDEV="--env-file .env.dev"
        info "Using study.json.dev..."
        STUDYJSON="study.json.dev"
    elif [ "$1" = "--feature" ]; then
        if [ -z "$2" ]; then
            fatal "--feature used without specifying a feature"
        fi
        FEATURE="--filter $2"
        info "Filtering by feature=$FEATURE"
    fi


    set -u

    if [[ $(docker-compose ps -q | wc -l) -ne 0 ]]; then
        info "Stopping existing services..."
        docker-compose $LOADDEV down
    fi

    if (dotnet tool list --global | grep livingdoc &>/dev/null); then
        info "Upgrading SpecFlow.Plus.LivingDoc.CLI..."
        dotnet tool update --global SpecFlow.Plus.LivingDoc.CLI
    else
        info "Installing SpecFlow.Plus.LivingDoc.CLI..."
        dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
    fi

    info "LOG_DIR = $LOG_DIR"
}

function build() {
    pushd $SCRIPT_DIR
    info "Building test runner..."
    dotnet build -c Release
}

function start_services() {
    info "Starting dependencies docker-compose $LOADDEV up -d --force-recreate..."
    docker-compose $LOADDEV up -d --force-recreate

    HOST_IP=$(docker network inspect testrunner | jq -r .[0].IPAM.Config[0].Gateway)
    info "Host IP = $HOST_IP"
    export HOST_IP

    info "============================================="
    docker container ls --format 'table {{.Names}}\t{{.ID}}' | grep integrationtest
    info "============================================="

    set +e
    COUNTER=0
    while true; do
        info "Waiting for Informatics Gateway ($COUNTER)..."
        count=$(curl -s http://$HOST_IP:5000/health/status | jq | grep "running" | wc -l)
        info "$count services running..."
        if [ $count -eq 6 ]; then
            break
        fi
        if [ $COUNTER -gt 100 ]; then
            fatal "Timeout waiting for Informatics Gateway ($COUNTER)."
        fi
        let COUNTER=COUNTER+1
        sleep 1s
    done
    set -e

    sleep 1
    sudo chown -R $USER:$USER $RUN_DIR
}

function write_da_metrics() {
    docker container list
    CID="$(docker container list | grep informatics-gateway | awk '{{print $1}}')"
    info "Streaming Informatics Gateway perf logs from container $CID to $METRICSFILE"

    until $EXIT; do
        DATA=$(docker stats $CID --no-stream --format "$(date +%s),{{.CPUPerc}},{{.MemUsage}},{{.NetIO}},{{.BlockIO}}")
        echo $DATA >>$METRICSFILE
        sleep 1
    done
}

function stream_da_metrics() {
    [ -f $METRICSFILE ] && sudo rm $METRICSFILE
    write_da_metrics &
    STREAMID=$!
}

function run_test() {
    pushd $TEST_DIR
    set +e
    info "Starting test runner..."

    if [[ "$VSTEST_HOST_DEBUG" == 0 ]]; then 
        dotnet test -c Release $FEATURE 2>&1 | tee $LOG_DIR/run.log
    else
        dotnet test -c Debug $FEATURE 2>&1 | tee $LOG_DIR/run.log
    fi
    EXITCODE=$?
    EXIT=true
    set -e
    popd
}

function generate_reports() {
    set +e
    info "Generating livingdoc..."
    pushd $BIN_DIR
    [ -f $BIN_DIR/TestExecution.json ] && mv $BIN_DIR/TestExecution.json $BIN_DIR/TestExecution_$(date +"%Y_%m_%d_%I_%M_%p").json
    livingdoc test-assembly Monai.Deploy.InformaticsGateway.Integration.Test.dll -t TestExecution*.json -o $LOG_DIR/
    popd
    set -e
}

function save_logs() {
    [ -d $RUN_DIR ] && info "Clearning $RUN_DIR..." && sudo rm -r $RUN_DIR
    info "Saving service log..."
    docker-compose $LOADDEV logs --no-color -t > "$LOG_DIR/services.log"
}

function tear_down() {
    set +e
    info "Stop streaming metrics log..."
    kill $STREAMID >/dev/null 2>&1
    set -e

    info "Stopping services..."
    docker-compose $LOADDEV down --remove-orphans
}

function main() {
    df -h
    env_setup "$@"
    build
    start_services
    stream_da_metrics
    run_test
    generate_reports
    df -h
    save_logs
    tear_down
    exit $EXITCODE
}

main "$@"
