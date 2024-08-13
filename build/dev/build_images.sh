#!/bin/bash

# Function to find the root directory by searching for the marker file
find_project_root() {
  local dir="$1"
  while [ "$dir" != "/" ]; do
    if [ -e "$dir/src" ]; then
      echo "$dir"
      return
    fi
    dir="$(dirname "$dir")"
  done
  echo "Error: project root not found" >&2
  exit 1
}

# Determine the directory where this script is located
SCRIPT_DIR="$(dirname "$(realpath "$0")")"
# Get the root directory of the project
ROOT_DIR=$(find_project_root "$SCRIPT_DIR")

# Change to the build/dev directory relative to the root directory
cd "$ROOT_DIR" || exit

# Function to convert the image name to the desired format
convert_image_name() {
  local image_name="$1"

  # Convert to lowercase
  local lower_name;
  local base_name;
  lower_name=$(echo "$image_name" | tr '[:upper:]' '[:lower:]')

  # If the image name ends with 'API', replace 'API' with an empty string and use underscores for remaining dots
  if [[ "$lower_name" == *"api" ]]; then
    base_name=$(echo "$lower_name" | sed 's/\.api$//' | sed 's/\./_/g')
  else
    # Otherwise, just replace dots with underscores
    base_name=$(echo "$lower_name" | sed 's/\./_/g')
  fi

  echo "$base_name"
}

# Function to build Docker images for .API projects and Kobalt.Bot
build_docker_images() {
  echo "Building Docker images for .API projects and Kobalt.Bot..."

  # Find all directories ending with .API and build Docker images
  find "$ROOT_DIR/src" -type d -name "*.API" | while read -r api_dir; do
    # Get the parent directory name which should be the project name
    project_name=$(basename "$(dirname "$api_dir")")
    echo "Building Docker image for $project_name..."

    # Build the Docker image
    docker build -t "$(convert_image_name "$(basename "${api_dir,,}")")" -f "$api_dir/Dockerfile" "$ROOT_DIR"

    echo "Built Docker image for $project_name"
  done

  # Build Docker image for Kobalt.Bot
  KOBALT_BOT_DIR="$ROOT_DIR/src/Kobalt/Kobalt.Bot"
  if [ -d "$KOBALT_BOT_DIR" ]; then
    echo "Building Docker image for Kobalt.Bot..."
    docker build -t "kobalt_bot" -f "$ROOT_DIR/src/Kobalt/Kobalt.Bot/Dockerfile" "$ROOT_DIR"
    echo "Built Docker image for Kobalt.Bot"
  else
    echo "Kobalt.Bot directory not found, skipping build."
  fi
}

# Call the function to build Docker images
build_docker_images
