#!/bin/zsh
#echo "Hello      World"
cd src/UI/WB.UI.Designer 
yarn
yarn gulp
cd ../WB.UI.Frontend
yarn
yarn build