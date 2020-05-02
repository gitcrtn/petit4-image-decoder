#!/usr/bin/env bash

ROOTDIR=$(dirname $(realpath $0))
cd ${ROOTDIR}

function check_cmd () {
    local name=$1; shift

    if !(type "${name}" > /dev/null 2>&1); then
        echo "Error: ${name} command not found"
        exit 1
    fi
}

function prepare_nuget () {
    if [ ! -e ${ROOTDIR}/nuget.exe ]; then
        check_cmd wget
        wget --no-check-certificate https://nuget.org/nuget.exe
    fi
}

function install_pkg () {
    local name=$1; shift
    local version=$1; shift

    if [ ! -e ${ROOTDIR}/${name}.${version} ]; then
        check_cmd mono
        prepare_nuget
        mono nuget.exe install ${name} -Version ${version}
        if [ ! -e ${ROOTDIR}/${name}.${version} ]; then
            echo "Error: Unable to find ${name}.${version}"
            exit 1
        fi
    fi
}

install_pkg CommandLineParser 2.7.82
install_pkg CoreCompat.System.Drawing 1.0.0-beta006

REF_ARG1=-reference:${ROOTDIR}/CommandLineParser.2.7.82/lib/net461/CommandLine.dll
REF_ARG2=-reference:${ROOTDIR}/CoreCompat.System.Drawing.1.0.0-beta006/lib/net45/CoreCompat.System.Drawing.dll

check_cmd mcs
mcs ${REF_ARG1} ${REF_ARG2} DecodePetit4Image.cs
