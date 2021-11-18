wsl --install -d ubuntu
until wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" wget https://github.com/microsoft/infersharp/releases/download/v1.2/infersharp-linux64-v1.2.tar.gz -O infersharp.tar.gz; do
    sleep 5
done
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" tar -xvzf infersharp.tar.gz
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" rm infersharp.tar.gz