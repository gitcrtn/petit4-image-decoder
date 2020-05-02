#!/usr/bin/env bash
if !(type "mono" > /dev/null 2>&1); then
    echo "Error: mono command not found"
    exit 1
fi

function prepare_dep ()
{
    local filename=$1; shift
    local filepath=$1; shift

    if [ ! -e ${ROOTDIR}/${filename} ]; then
        cp ${filepath} ${ROOTDIR}
    fi
}

ROOTDIR=$(dirname $(realpath $0))

prepare_dep CommandLine.dll ${ROOTDIR}/CommandLineParser.2.7.82/lib/net461/CommandLine.dll
prepare_dep CoreCompat.System.Drawing.dll ${ROOTDIR}/CoreCompat.System.Drawing.1.0.0-beta006/lib/net45/CoreCompat.System.Drawing.dll

mono DecodePetit4Image.exe "$@"
