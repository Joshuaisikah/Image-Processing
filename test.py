#!/usr/bin/env python3
"""
Crash-recovery test for the Image Processing Service.
Posts a batch of images, kills the app mid-processing, restarts it,
and confirms every image eventually reaches Completed or Failed.

Run this from the repo root (~/dotnet/Imageprocessing).

Requires: requests  (pip install requests)
Assumes: Imageprocessing.Api has already been built (dotnet build),
         and Postgres is running with migrations applied.
"""

import subprocess
import time
import signal
import os
import requests
import sys
from pathlib import Path

API_URL = "http://localhost:5000"  # adjust to your actual port
API_PROJECT_DIR = "Imageprocessing.Api"  # relative to repo root
DLL_RELATIVE_PATH = "bin/Debug/net10.0/Imageprocessing.Api.dll"  # adjust framework folder if needed
NUM_IMAGES = 200
TEST_IMAGE_PATH = "test.jpg"  # a small valid jpeg sitting next to this script
POLL_TIMEOUT_SECONDS = 300
POLL_INTERVAL_SECONDS = 2
STARTUP_TIMEOUT_SECONDS = 30

# While debugging startup issues, keep this False so you can see the app's
# own console output. Once everything is confirmed working, you can set this
# to True to silence it for a cleaner run.
SILENCE_APP_OUTPUT = False


def start_app():
    print("Starting app...")

    dll_path = Path(API_PROJECT_DIR) / DLL_RELATIVE_PATH
    if not dll_path.exists():
        raise FileNotFoundError(
            f"DLL not found at {dll_path}. Run `dotnet build` first, "
            f"and check DLL_RELATIVE_PATH matches your target framework."
        )

    env = os.environ.copy()
    env["ASPNETCORE_ENVIRONMENT"] = "Development"

    stdout = subprocess.DEVNULL if SILENCE_APP_OUTPUT else None
    stderr = subprocess.DEVNULL if SILENCE_APP_OUTPUT else None

    proc = subprocess.Popen(
        ["dotnet", DLL_RELATIVE_PATH],
        cwd=API_PROJECT_DIR,
        env=env,
        stdout=stdout,
        stderr=stderr,
    )

    start = time.time()
    while time.time() - start < STARTUP_TIMEOUT_SECONDS:
        # if the process already died, no point waiting further
        if proc.poll() is not None:
            raise RuntimeError(
                f"App process exited early with code {proc.returncode} "
                f"before it started accepting requests."
            )
        try:
            requests.get(
                f"{API_URL}/api/images/00000000-0000-0000-0000-000000000000",
                timeout=1,
            )
            print(f"App started, PID {proc.pid}")
            return proc
        except requests.exceptions.ConnectionError:
            time.sleep(1)

    raise RuntimeError("App did not start in time")


def kill_app(proc):
    print(f"Killing app (PID {proc.pid}) with SIGKILL...")
    try:
        proc.send_signal(signal.SIGKILL)
        proc.wait(timeout=10)
    except ProcessLookupError:
        pass  # already dead
    print("App killed.")


def upload_images(n):
    ids = []
    with open(TEST_IMAGE_PATH, "rb") as f:
        image_bytes = f.read()

    for i in range(n):
        files = {"file": (f"test-{i}.jpg", image_bytes, "image/jpeg")}
        resp = requests.post(f"{API_URL}/api/images", files=files)
        if resp.status_code != 202:
            print(f"  WARNING: upload {i} returned {resp.status_code}: {resp.text}")
            continue
        ids.append(resp.json()["id"])

    print(f"Uploaded {len(ids)}/{n} images.")
    return ids


def poll_all(ids, timeout, interval):
    remaining = set(ids)
    completed = {}
    start = time.time()
    connection_error_printed = False

    while remaining and (time.time() - start) < timeout:
        for image_id in list(remaining):
            try:
                resp = requests.get(f"{API_URL}/api/images/{image_id}", timeout=5)
            except requests.exceptions.ConnectionError as e:
                if not connection_error_printed:
                    print(f"  connection error polling {image_id}: {e}")
                    connection_error_printed = True
                continue
            if resp.status_code != 200:
                print(f"  unexpected status {resp.status_code} for {image_id}: {resp.text}")
                continue
            status = resp.json()["status"]
            if status in ("Completed", "Failed"):
                completed[image_id] = status
                remaining.discard(image_id)
        if remaining:
            time.sleep(interval)

    return completed, remaining


def main():
    if not Path(TEST_IMAGE_PATH).exists():
        print(f"Missing {TEST_IMAGE_PATH} — put a small valid jpeg next to this script.")
        sys.exit(1)

    proc = start_app()

    ids = upload_images(NUM_IMAGES)
    if not ids:
        print("No images uploaded successfully, aborting.")
        kill_app(proc)
        sys.exit(1)

    # Kill almost immediately — don't wait, to maximize the chance of
    # catching images still Pending/Processing.
    time.sleep(1)
    kill_app(proc)

    print("Restarting app to trigger recovery...")
    proc = start_app()

    print("Polling all images until they resolve...")
    completed, stuck = poll_all(ids, POLL_TIMEOUT_SECONDS, POLL_INTERVAL_SECONDS)

    kill_app(proc)

    print("\n--- Results ---")
    print(f"Total uploaded:  {len(ids)}")
    print(f"Resolved:        {len(completed)}")
    print(f"Still stuck:     {len(stuck)}")

    if stuck:
        print(f"\nSTUCK IDs (never reached Completed/Failed): {stuck}")
        sys.exit(1)

    failed = [i for i, s in completed.items() if s == "Failed"]
    if failed:
        print(f"\n{len(failed)} images ended as Failed (may be expected or a bug depending on why): {failed}")

    print("\nAll images resolved. Crash recovery test PASSED.")


if __name__ == "__main__":
    main()
