/* Copyright 2019-2021 Canaan Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#include "kernel_test.h"
#include <gtest/gtest.h>
#include <iostream>
#include <nncase/kernels/stackvm/tensor_ops.h>
#include <nncase/runtime/datatypes.h>
#include <nncase/runtime/runtime_tensor.h>
#include <nncase/runtime/simple_types.h>
#include <nncase/runtime/stackvm/opcode.h>
#include <ortki/operators.h>

using namespace nncase;
using namespace nncase::runtime;
using namespace ortki;

class LrnTest
    : public KernelTest,
      public ::testing::TestWithParam<std::tuple<nncase::typecode_t, dims_t>> {
  public:
    void SetUp() override {
        auto &&[typecode, l_shape] = GetParam();

        input =
            hrt::create(typecode, l_shape, host_runtime_tensor::pool_cpu_only)
                .expect("create tensor failed");
        init_tensor(input);
    }

    void TearDown() override {}

    virtual void init_tensor(runtime::runtime_tensor &tensor) override {
        auto dtype = tensor.datatype();
        switch (dtype) {
        case dt_int8: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<> dis(-6, 6);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<int8_t>(tensor, index) = static_cast<int8_t>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_int16: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<> dis(-6, 6);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<int16_t>(tensor, index) =
                        static_cast<int16_t>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_int32: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<> dis(-6, 6);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<int32_t>(tensor, index) = dis(gen);
                    return ok();
                });
            break;
        }
        case dt_int64: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<> dis(-6, 6);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<int64_t>(tensor, index) =
                        static_cast<int64_t>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_uint8: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<> dis(0, 127);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<uint8_t>(tensor, index) =
                        static_cast<uint8_t>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_uint16: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<> dis(0, 127);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<uint16_t>(tensor, index) =
                        static_cast<uint16_t>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_uint32: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<> dis(0, 127);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<uint32_t>(tensor, index) =
                        static_cast<uint32_t>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_uint64: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_int_distribution<uint64_t> dis(0, 127);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<uint64_t>(tensor, index) =
                        static_cast<uint64_t>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_float16: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_real_distribution<float> dis(-1.0f, 1.0f);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<half>(tensor, index) = static_cast<half>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_float32: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_real_distribution<float> dis(-100.0f, 100.0f);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<float>(tensor, index) = static_cast<float>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_float64: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_real_distribution<double> dis(-1.0, 1.0);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<double>(tensor, index) = static_cast<double>(dis(gen));
                    return ok();
                });
            break;
        }
        case dt_boolean: {
            std::random_device rd;
            std::mt19937 gen(rd());
            std::uniform_real_distribution<double> dis(-1.0, 1.0);
            NNCASE_UNUSED auto res = kernels::stackvm::apply(
                tensor.shape(),
                [&](gsl::span<const size_t> index) -> result<void> {
                    get<bool>(tensor, index) =
                        static_cast<double>(dis(gen)) >= 0;
                    return ok();
                });
            break;
        }
        default: {
        }
        }
    }

  protected:
    runtime_tensor input;
};

INSTANTIATE_TEST_SUITE_P(lrn, LrnTest,
                         testing::Combine(testing::Values(dt_float32),
                                          testing::Values(dims_t{1, 3, 16,
                                                                 16})));

TEST_P(LrnTest, lrn) {
    auto l_ort = runtime_tensor_2_ort_tensor(input);

    // expected
    auto output_ort = ortki_LRN(l_ort, 0.22f, 0.20f, 0.75f, 3);
    size_t size = 0;
    void *ptr_ort = tensor_buffer(output_ort, &size);
    dims_t shape(tensor_rank(output_ort));
    tensor_shape(output_ort, reinterpret_cast<int64_t *>(shape.data()));
    auto expected = hrt::create(input.datatype(), shape,
                                {reinterpret_cast<gsl::byte *>(ptr_ort), size},
                                true, host_runtime_tensor::pool_cpu_only)
                        .expect("create tensor failed");

    // actual
    float_t alpha_ptr[] = {0.22f};
    auto alpha = hrt::create(dt_float32, {1},
                             {reinterpret_cast<gsl::byte *>(alpha_ptr),
                              sizeof(alpha_ptr)},
                             true, host_runtime_tensor::pool_cpu_only)
                     .expect("create tensor failed");
    float_t beta_ptr[] = {0.20f};
    auto beta =
        hrt::create(dt_float32, {1},
                    {reinterpret_cast<gsl::byte *>(beta_ptr), sizeof(beta_ptr)},
                    true, host_runtime_tensor::pool_cpu_only)
            .expect("create tensor failed");
    float_t bias_ptr[] = {0.75f};
    auto bias =
        hrt::create(dt_float32, {1},
                    {reinterpret_cast<gsl::byte *>(bias_ptr), sizeof(bias_ptr)},
                    true, host_runtime_tensor::pool_cpu_only)
            .expect("create tensor failed");
    int64_t size_ptr[] = {3l};
    auto size0 =
        hrt::create(dt_int64, {1},
                    {reinterpret_cast<gsl::byte *>(size_ptr), sizeof(size_ptr)},
                    true, host_runtime_tensor::pool_cpu_only)
            .expect("create tensor failed");
    auto output = kernels::stackvm::lrn(input.impl(), alpha.impl(), beta.impl(),
                                        bias.impl(), size0.impl())
                      .expect("lrn failed");
    runtime_tensor actual(output.as<tensor>().expect("as tensor failed"));

    print_runtime_tensor(actual);

//    bool result = is_same_tensor(expected, actual) ||
//                  cosine_similarity_tensor(expected, actual);
//
//    if (!result) {
//        print_runtime_tensor(actual);
//        print_runtime_tensor(expected);
//    }
//
//    // compare
//    EXPECT_TRUE(result);
}

int main(int argc, char *argv[]) {
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}