# Copyright (c) 2018-present, Facebook, Inc.
#
# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this source tree.

IPHONESIMULATOR_ISYSROOT = $(XCODE_BASE)/Platforms/iPhoneSimulator.platform/Developer/SDKs/iPhoneSimulator.sdk

OBJC_TARGET = x86_64-apple-darwin14

IOS_SIMULATOR_VERSION = 8.2

IOS_CLANG_OPTIONS = -isysroot $(IPHONESIMULATOR_ISYSROOT) \
	-target $(OBJC_TARGET) -mios-simulator-version-min=$(IOS_SIMULATOR_VERSION)

OBJC_CLANG_OPTIONS = $(IOS_CLANG_OPTIONS) -x objective-c

OBJCPP_CLANG_OPTIONS = $(IOS_CLANG_OPTIONS) -x objective-c++ -std=c++11
