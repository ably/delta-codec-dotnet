# Delta Codec Test Suite

This directory contains comprehensive tests for the VCDIFF decoder implementation.

## Test Structure

### 1. VcdiffDecoderFixture.cs
Legacy tests using the original xdelta test data located in `TestData/xdelta/`. These tests validate basic decoder functionality with 4 test cases.

### 2. VcdiffTestSuiteFixture.cs (NEW)
Comprehensive test suite using the [vcdiff-tests](https://github.com/ably/vcdiff-tests) submodule. This provides **85 standardized tests** across multiple categories:

#### Test Categories

**Targeted Positive Tests** (29 tests)
- Basic operations: empty files, content changes, duplications, unchanged files
- Varint boundary tests: All varint encoding boundaries (0, 127, 128, 16383, 16384, 2097151, 2097152) for ADD, COPY, and RUN instructions
- Codetable tests: Complete coverage of all 256 VCDIFF codetable entries
- Address cache tests: Near cache, same cache, different addressing modes
- Checksum tests: VCD_ADLER32 validation

**Targeted Negative Tests** (33 tests)
- Invalid headers: Wrong magic bytes, unsupported versions
- Malformed windows: Invalid indicators, impossible sizes
- Bad instructions: Invalid instruction codes, out-of-bounds references
- Checksum failures: Incorrect Adler32 checksums
- Format violations: Truncated files, invalid varints

**General Positive Tests** (20 tests)
- Binary files: 64 bytes, 1KB, 64KB with append/delete/insert/modify operations
- JSON files: 1KB, 64KB with structure-preserving modifications

**Fuzz Tests** (optional)
- Corrupted input validation (ensures decoder doesn't crash)

## Test Format

Each test case in the vcdiff-tests submodule consists of:
- `source` - Original file (may be empty)
- `target` - Expected result after applying delta
- `delta.vcdiff` - VCDIFF delta file
- `metadata.json` - Test description and expected behavior

## Running Tests

Run all tests:
```bash
dotnet test
```

Run only the new comprehensive test suite:
```bash
dotnet test --filter "FullyQualifiedName~VcdiffTestSuiteFixture"
```

Run specific test categories:
```bash
# Targeted positive tests
dotnet test --filter "FullyQualifiedName~VcdiffTestSuiteFixture.TestTargetedPositive"

# Targeted negative tests
dotnet test --filter "FullyQualifiedName~VcdiffTestSuiteFixture.TestTargetedNegative"

# General positive tests
dotnet test --filter "FullyQualifiedName~VcdiffTestSuiteFixture.TestGeneralPositive"
```

## Test Results

Current test status:
- ✅ **All tests passing**

All 85 tests from the comprehensive vcdiff-tests suite are passing successfully, along with the 4 legacy xdelta tests.

## Updating Test Data

The vcdiff-tests submodule can be updated to get the latest test cases:

```bash
cd IO.Ably.DeltaCodec.Test/vcdiff-tests
git pull origin main
cd ../..
git add IO.Ably.DeltaCodec.Test/vcdiff-tests
git commit -m "Update vcdiff-tests submodule"
```

## Implementation Details

The test implementation in `VcdiffTestSuiteFixture.cs` mirrors the Go implementation from [vcdiff-go](https://github.com/ably/vcdiff-go), providing:

1. **Automatic test discovery**: Recursively finds all test cases in each category
2. **Metadata parsing**: Reads and validates test metadata from JSON files
3. **Dynamic test generation**: Creates NUnit test cases for each discovered test
4. **Comprehensive validation**: Compares decoded output byte-by-byte with expected targets
5. **Error handling validation**: Ensures negative tests fail as expected

## References

- [RFC 3284: VCDIFF Format Specification](https://tools.ietf.org/html/rfc3284)
- [vcdiff-tests Repository](https://github.com/ably/vcdiff-tests)
- [vcdiff-go Implementation](https://github.com/ably/vcdiff-go)