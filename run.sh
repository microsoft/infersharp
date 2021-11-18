wsl -d ubuntu -u root cp -r . //infer-staging ||
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" cp infersharp/Cilsil/System.Private.CoreLib.dll //infer-staging ||
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" infersharp/Cilsil/Cilsil translate //infer-staging --outcfg //cfg.json --outtenv //tenv.json ||

# Run Infer's analysis
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" rm -rf infer-out/captured
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" infersharp/infer/lib/infer/infer/bin/infer capture
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" mkdir -p infer-out/captured
wsl -d ubuntu -u root --cd "//wsl/Ubuntu/home/root/root" infersharp/infer/lib/infer/infer/bin/infer analyzejson --pulse --no-biabduction --cfg-json cfg.json --tenv-json tenv.json
wsl -d ubuntu -u root cp -r //infer-out/ .