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
#pragma once
#include "../../ukernels.h"
#include "arch_types.h"
#include "nncase/ntt/vector.h"
#include <vector>

namespace nncase::ntt::ukernels {
template <size_t M, size_t N, size_t MStrides>
class u_pack<M, N, MStrides, true, float, vector<float, 8>> {
  public:
    constexpr void operator()(const float *input,
                              vector<float, 8> *output) noexcept {
        for (size_t j = 0; j < N; j++) {
            for (size_t i = 0; i < M; i++) {
                output[j](i) = input[i * MStrides + j];
            }
        }

        if constexpr (M < 8) {
            for (size_t j = 0; j < N; j++) {
                for (size_t i = M; i < 8; i++) {
                    output[j](i) = 0.f;
                }
            }
        }
    }
};

template <reduce_op Op, class T> struct u_reduce_policy<Op, T, true> {
    static constexpr size_t unroll = 8;
};
} // namespace nncase::ntt::ukernels
